using AutoMapper;
using Helpdesk.DTOs;
using Helpdesk.Enums;
using Helpdesk.Exceptions;
using Helpdesk.Helpers;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Helpdesk.Services
{
    public class TicketService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditLogService auditLogService,
        ICurrentUserService currentUser,
        ISlaCalculationEngine slaEngine,
        INotificationService notificationService,
        IConfiguration configuration) : ITicketService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly IAuditLogService _auditLogService = auditLogService;
        private readonly ICurrentUserService _currentUser = currentUser;
        private readonly ISlaCalculationEngine _slaEngine = slaEngine;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IConfiguration _configuration = configuration;

      
        private async Task<List<int>> GetManagedDepartmentIdsAsync(int userId)
        {
            return await _unitOfWork.Departments.Query()
                .Where(d => d.DepartmentHeadId == userId)
                .Select(d => d.Id)
                .ToListAsync();
        }

        private static IQueryable<Ticket> ApplyVisibilityFilter(
            IQueryable<Ticket> query,
            int userId,
            UserRole role,
            List<int> managedDeptIds)
        {
            return role switch
            {
                UserRole.Admin => query,

                UserRole.DepartmentHead => query.Where(t =>
                    t.CreatedByUserId == userId ||
                    t.RaisedForUserId == userId ||
                    managedDeptIds.Contains(t.DepartmentId)),

                UserRole.Agent => managedDeptIds.Count > 0
                    ? query.Where(t =>
                        t.AssignedToAgentId == userId ||
                        t.CreatedByUserId == userId ||
                        t.RaisedForUserId == userId ||
                        managedDeptIds.Contains(t.DepartmentId))
                    : query.Where(t =>
                        t.AssignedToAgentId == userId ||
                        t.CreatedByUserId == userId ||
                        t.RaisedForUserId == userId),

                _ /* UserRole.User */ => managedDeptIds.Count > 0
                    ? query.Where(t =>
                        t.CreatedByUserId == userId ||
                        t.RaisedForUserId == userId ||
                        managedDeptIds.Contains(t.DepartmentId))
                    : query.Where(t =>
                        t.CreatedByUserId == userId ||
                        t.RaisedForUserId == userId),
            };
        }

    
        private static bool CanAccess(
            Ticket ticket,
            int userId,
            UserRole role,
            List<int> managedDeptIds)
        {
            return role switch
            {
                UserRole.Admin => true,
                UserRole.DepartmentHead =>
                    ticket.CreatedByUserId == userId ||
                    ticket.RaisedForUserId == userId ||
                    managedDeptIds.Contains(ticket.DepartmentId),
                UserRole.Agent =>
                    ticket.AssignedToAgentId == userId ||
                    ticket.CreatedByUserId == userId ||
                    managedDeptIds.Contains(ticket.DepartmentId),
                _ =>
                    ticket.CreatedByUserId == userId ||
                    ticket.RaisedForUserId == userId ||
                    managedDeptIds.Contains(ticket.DepartmentId),
            };
        }

        private bool CanModify(Ticket ticket)
        {
            return _currentUser.UserRole == UserRole.Admin ||
                   ticket.CreatedByUserId == _currentUser.UserId ||
                   (_currentUser.UserRole == UserRole.Agent &&
                    ticket.AssignedToAgentId == _currentUser.UserId);
        }


        public async Task<PagedResponse<TicketResponseDto>> FilterTicketsAsync(TicketFilterDto filter)
        {
            var userId = _currentUser.UserId;
            var role = _currentUser.UserRole;

            // Single dept-head lookup for the request
            var managedDeptIds = await GetManagedDepartmentIdsAsync(userId);

            var query = _unitOfWork.Tickets.Query();
            query = ApplyVisibilityFilter(query, userId, role, managedDeptIds);

            query = query
                .Include(t => t.CreatedByUser)
                .Include(t => t.RaisedForUser)
                .Include(t => t.AssignedToAgent)
                .Include(t => t.Category)
                .Include(t => t.Department);

            query = query.Where(t => t.Status != TicketStatus.Archived);

            if (filter.Status.HasValue)
                query = query.Where(t => t.Status == filter.Status.Value);

            if (filter.Priority.HasValue)
                query = query.Where(t => t.Priority == filter.Priority.Value);

            if (filter.CategoryId.HasValue)
                query = query.Where(t => t.CategoryId == filter.CategoryId.Value);

            if (filter.AssignedToAgentId.HasValue)
            {
                if (filter.AssignedToAgentId.Value == -1)
                    query = query.Where(t => t.AssignedToAgentId == null);
                else
                    query = query.Where(t => t.AssignedToAgentId == filter.AssignedToAgentId.Value);
            }

            if (filter.RaisedByUserId.HasValue)
            {
                query = query.Where(t => t.CreatedByUserId == filter.RaisedByUserId.Value || t.RaisedForUserId == filter.RaisedByUserId.Value);
            }

           
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(search) ||
                    t.Description.ToLower().Contains(search));
            }

            query = query
                .OrderByDescending(t => t.IsEscalated && !t.EscalationAcknowledgedAt.HasValue)
                .ThenByDescending(t => t.EscalatedAt)
                .ThenByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync();

            var tickets = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResponse<TicketResponseDto>
            {
                Data = _mapper.Map<IEnumerable<TicketResponseDto>>(tickets),
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        public async Task<TicketResponseDto?> GetTicketByIdAsync(GetTicketByIdDto dto)
        {
            var ticket = await _unitOfWork.Tickets
                .GetByIdWithIncludeAsync(dto.Id,
                    t => t.CreatedByUser,
                    t => t.RaisedForUser,
                    t => t.AssignedToAgent,
                    t => t.Category,
                    t => t.Department);

            if (ticket == null)
                return null;

            
            var managedDeptIds = await GetManagedDepartmentIdsAsync(dto.UserId);

            if (!CanAccess(ticket, dto.UserId, dto.Role, managedDeptIds))
                return null;

            var response = _mapper.Map<TicketResponseDto>(ticket);
            await PopulateSlaResponseAsync(ticket, response);
            return response;
        }

        public async Task<int> CreateTicketAsync(CreateTicketDto dto)
        {
            var ticket = _mapper.Map<Ticket>(dto);
            var currentUserId = _currentUser.UserId;

            if (!Enum.TryParse<UserRole>(_currentUser.Role, true, out var role))
                throw new InvalidOperationException("Invalid role");

            if (currentUserId <= 0)
                throw new InvalidOperationException("Invalid user");

            if (role == UserRole.Admin)
            {
                ticket.CreatedByUserId = currentUserId;
                ticket.RaisedForUserId = dto.RaisedForUserId ?? currentUserId;
                ticket.AssignedToAgentId = dto.AssignedToAgentId;
            }
            else
            {
                ticket.CreatedByUserId = currentUserId;
                ticket.RaisedForUserId = currentUserId;
            }

            ticket.Status = TicketStatus.Open;
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.LastUpdatedAt = DateTime.UtcNow;
            ticket.SLADeadline = await _slaEngine.CalculateDeadlineAsync(ticket.CreatedAt, ticket.Priority);

            if (ticket.CategoryId <= 0)
                throw new InvalidOperationException("Invalid Category");

            if (ticket.DepartmentId <= 0)
            {
                ticket.DepartmentId = await ResolveDepartmentIdAsync(ticket.RaisedForUserId, ticket.CreatedByUserId);

                if (ticket.DepartmentId <= 0)
                    throw new InvalidOperationException("A valid active Department is required");
            }
            else if (!await _unitOfWork.Departments.Query()
                .AnyAsync(d => d.Id == ticket.DepartmentId && d.IsActive))
            {
                throw new InvalidOperationException("A valid active Department is required");
            }

            if (ticket.RelatedTicketId > 0
                && !await _unitOfWork.Tickets.ExistsAsync(ticket.RelatedTicketId.Value))
            {
                ticket.RelatedTicketId = null;
            }

            if (ticket.AssignedToAgentId > 0
                && !await _unitOfWork.Users.ExistsAsync(ticket.AssignedToAgentId.Value))
            {
                ticket.AssignedToAgentId = null;
            }

            if (ticket.RaisedForUserId > 0
                && !await _unitOfWork.Users.ExistsAsync(ticket.RaisedForUserId.Value))
            {
                throw new InvalidOperationException($"User with ID {ticket.RaisedForUserId} does not exist");
            }

            await _unitOfWork.Tickets.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogTicketCreatedAsync(ticket.Id, $"Ticket '{ticket.Title}' created", currentUserId);

            
            var notifyUserId = ticket.RaisedForUserId ?? ticket.CreatedByUserId;
            User notifyUser = null;
            if (notifyUserId > 0)
            {
                notifyUser = await _unitOfWork.Users.GetByIdAsync(notifyUserId);
                if (notifyUser != null)
                    await _notificationService.SendTicketCreatedAsync(notifyUser, ticket);
            }

            var adminEmail = _configuration["AdminSettings:Email"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                var adminUser = await _unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.Email == adminEmail);
                if (adminUser != null)
                {
                    await _notificationService.SendTicketCreatedAsync(adminUser, ticket);
                }
                else
                {
                    var fallbackAdmin = new User { Email = adminEmail, FirstName = "Admin" };
                    await _notificationService.SendTicketCreatedAsync(fallbackAdmin, ticket);
                }
            }

            if (ticket.AssignedToAgentId.HasValue)
            {
                var assignedAgent = await _unitOfWork.Users.GetByIdAsync(ticket.AssignedToAgentId.Value);
                if (assignedAgent != null)
                {
                    await _notificationService.SendAssignmentAsync(assignedAgent, ticket);
                    if (notifyUser != null)
                        await _notificationService.SendAssignmentAsync(notifyUser, ticket);
                }
            }

            return ticket.Id;
        }

        public async Task<bool> DeleteTicketAsync(GetByIdDto dto)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(dto.Id);

            if (ticket == null)
                return false;

            if (!CanModify(ticket))
                return false;

            if (ticket.Status == TicketStatus.Archived)
                return true;

            ticket.Status = TicketStatus.Archived;
            ticket.LastUpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Tickets.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogTicketArchivedAsync(ticket.Id, _currentUser.UserId);

            return true;
        }

        public async Task<TicketResponseDto?> UpdateTicketAsync(UpdateTicketDto dto)
        {
            var ticket = await _unitOfWork.Tickets
                .GetByIdWithIncludeAsync(dto.Id,
                    t => t.CreatedByUser,
                    t => t.RaisedForUser,
                    t => t.AssignedToAgent,
                    t => t.Category);

            if (ticket == null)
                throw new NotFoundException("Ticket", dto.Id);

            if (ticket.Status == TicketStatus.Archived)
                throw new InvalidOperationException("Archived tickets are read-only.");

            if (!CanModify(ticket))
                throw new ForbiddenException("You cannot modify this ticket");

            var oldStatus = ticket.Status;
            var oldPriority = ticket.Priority;
            var oldAgentId = ticket.AssignedToAgentId;

            _mapper.Map(dto, ticket);

            if (dto.Status.HasValue && oldStatus != dto.Status.Value)
                await HandleStatusTransitionAsync(ticket, oldStatus, dto.Status.Value);

            if (dto.Priority.HasValue && oldPriority != dto.Priority.Value)
                await RecalculateSlaInternalAsync(ticket, "Priority changed");

            ticket.LastUpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Tickets.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            await HandlePostUpdateNotificationsAsync(ticket, oldStatus, oldAgentId);

           
            var updated = await _unitOfWork.Tickets
                .GetByIdWithIncludeAsync(dto.Id,
                    t => t.CreatedByUser,
                    t => t.RaisedForUser,
                    t => t.AssignedToAgent,
                    t => t.Category,
                    t => t.Department);

            if (updated == null)
                throw new NotFoundException("Ticket was updated but could not be retrieved", dto.Id);

            var response = _mapper.Map<TicketResponseDto>(updated);
            await PopulateSlaResponseAsync(updated, response);
            return response;
        }

        public async Task<bool> EscalateTicketAsync(EscalateTicketDto dto)
        {
            var ticket = await _unitOfWork.Tickets
                .GetByIdWithIncludeAsync(dto.TicketId,
                    t => t.AssignedToAgent,
                    t => t.Department,
                    t => t.Department.DepartmentHead);

            if (ticket == null)
                throw new NotFoundException("Ticket", dto.TicketId);

            var role = _currentUser.Role;
            bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isAssignedAgent = string.Equals(role, "Agent", StringComparison.OrdinalIgnoreCase)
                && ticket.AssignedToAgentId == _currentUser.UserId;

            if (!isAdmin && !isAssignedAgent
                && (string.IsNullOrEmpty(dto.Reason)
                    || dto.Reason != "System Auto-Escalation (Reopened 3+ times by user)"))
            {
                throw new ForbiddenException("Only Admins or the Assigned Agent can escalate this ticket.");
            }

            if (!ticket.CanEscalateInCurrentStatus())
                throw new InvalidOperationException("Ticket is already escalated in its current status cycle.");

            var oldPriority = ticket.Priority;
            ticket.BumpPriority();

            if (oldPriority != ticket.Priority)
                ticket.SLADeadline = await _slaEngine.CalculateDeadlineAsync(DateTime.UtcNow, ticket.Priority);

            ticket.IsEscalated = true;
            ticket.LastEscalatedStatus = ticket.Status;
            ticket.EscalatedAt = DateTime.UtcNow;
            ticket.EscalationReason = dto.Reason;

            await _unitOfWork.Tickets.UpdateAsync(ticket);

            await _auditLogService.LogTicketEscalatedAsync(ticket.Id, dto.Reason ?? "", _currentUser.UserId);

            // FIX: save before sending notifications so the state is persisted
            // even if a notification transport fails.
            await _unitOfWork.SaveChangesAsync();

            // Notify Admin
            var adminEmail = _configuration["AdminSettings:Email"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                var adminUser = await _unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.Email == adminEmail);
                if (adminUser != null)
                {
                    await _notificationService.SendEscalationAsync(adminUser, ticket, dto.Reason ?? "");
                }
                else
                {
                    var fallbackAdmin = new User { Email = adminEmail, FirstName = "Admin" };
                    await _notificationService.SendEscalationAsync(fallbackAdmin, ticket, dto.Reason ?? "");
                }
            }

            // Notify Department Head
            if (ticket.Department?.DepartmentHead != null)
                await _notificationService.SendEscalationAsync(ticket.Department.DepartmentHead, ticket, dto.Reason ?? "");

            return true;
        }

        public async Task<bool> AcknowledgeEscalationAsync(int ticketId)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null)
                throw new NotFoundException("Ticket", ticketId);

            if (ticket.Status == TicketStatus.Archived)
                throw new InvalidOperationException("Archived tickets are read-only.");

            if (!string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("Only Admins can acknowledge escalations.");

            if (!ticket.IsEscalated || ticket.EscalationAcknowledgedAt.HasValue)
                return false;

            ticket.EscalationAcknowledgedAt = DateTime.UtcNow;

            await _unitOfWork.Tickets.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ReopenTicketAsync(int ticketId)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null)
                throw new NotFoundException("Ticket", ticketId);

            var isAdmin = string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            var isAssignedAgent = string.Equals(_currentUser.Role, "Agent", StringComparison.OrdinalIgnoreCase)
                && ticket.AssignedToAgentId == _currentUser.UserId;
            var isRequester = ticket.RaisedForUserId == _currentUser.UserId || ticket.CreatedByUserId == _currentUser.UserId;

            if (!isAdmin && !isAssignedAgent && !isRequester)
                throw new ForbiddenException("Only the requester, assigned agent, or an Admin can reopen this ticket.");

            if (ticket.Status != TicketStatus.Closed && ticket.Status != TicketStatus.Resolved)
                throw new InvalidOperationException("Only Closed or Resolved tickets can be reopened.");

            var oldStatus = ticket.Status;
            ticket.Status = TicketStatus.Reopened;
            ticket.LastUpdatedAt = DateTime.UtcNow;
            ticket.SLADeadline = await _slaEngine.CalculateDeadlineAsync(DateTime.UtcNow, ticket.Priority);
            ticket.IsEscalated = false;
            ticket.EscalationReason = null;
            ticket.EscalatedAt = null;
            ticket.EscalationAcknowledgedAt = null;

            // FIX: reopen count must always increment (not just for UserRole.User)
            // so HasReachedReopenLimit() can fire auto-escalation correctly.
            ticket.IncrementReopenCount();

            await _unitOfWork.Tickets.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            if (ticket.HasReachedReopenLimit())
            {
                var escalateDto = new EscalateTicketDto
                {
                    TicketId = ticket.Id,
                    Reason = "System Auto-Escalation (Reopened 3+ times by user)"
                };
                await EscalateTicketAsync(escalateDto);
            }

            await _auditLogService.LogTicketStatusChangedAsync(
                ticket.Id, oldStatus.ToString(), ticket.Status.ToString(), _currentUser.UserId);

            var notifyUserId = ticket.RaisedForUserId ?? ticket.CreatedByUserId;
            if (notifyUserId > 0)
            {
                var notifyUser = await _unitOfWork.Users.GetByIdAsync(notifyUserId);
                if (notifyUser != null)
                    await _notificationService.SendStatusChangedAsync(notifyUser, ticket, oldStatus.ToString());
            }

            var reopenActor = !string.IsNullOrEmpty(_currentUser.FullName) ? _currentUser.FullName : (_currentUser.Email ?? "User/Agent");
            var reason = $"Reopened by {reopenActor}";

            // Notify Admin
            var adminEmail = _configuration["AdminSettings:Email"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                var adminUser = await _unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.Email == adminEmail);
                if (adminUser != null)
                {
                    await _notificationService.SendTicketReopenedAsync(adminUser, ticket, reason);
                }
                else
                {
                    var fallbackAdmin = new User { Email = adminEmail, FirstName = "Admin" };
                    await _notificationService.SendTicketReopenedAsync(fallbackAdmin, ticket, reason);
                }
            }

            // Notify Assigned Agent
            if (ticket.AssignedToAgentId.HasValue)
            {
                var assignedAgent = await _unitOfWork.Users.GetByIdAsync(ticket.AssignedToAgentId.Value);
                if (assignedAgent != null)
                    await _notificationService.SendTicketReopenedAsync(assignedAgent, ticket, reason);
            }

            return true;
        }

        public async Task<bool> OverrideSlaAsync(int ticketId, OverrideSlaDto dto)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null)
                throw new NotFoundException("Ticket", ticketId);

            if (!string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("Only Admins can override SLA deadlines.");

            var oldDeadline = ticket.SLADeadline;
            ticket.SLADeadline = dto.NewSlaDeadline;
            ticket.LastUpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Tickets.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogSlaOverriddenAsync(
                ticket.Id,
                oldDeadline?.ToString() ?? "None",
                ticket.SLADeadline?.ToString() ?? "None",
                dto.Reason ?? "",
                _currentUser.UserId);

            var notifyUserId = ticket.AssignedToAgentId ?? ticket.CreatedByUserId;
            if (notifyUserId > 0)
            {
                var notifyUser = await _unitOfWork.Users.GetByIdAsync(notifyUserId);
                if (notifyUser != null)
                    await _notificationService.SendEscalationAsync(notifyUser, ticket, dto.Reason ?? "");
            }

            return true;
        }

        public async Task<bool> ResolveViaKBAsync(int ticketId, int kbArticleId)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null)
                throw new NotFoundException("Ticket", ticketId);

            var kbArticle = await _unitOfWork.KBArticles.GetByIdAsync(kbArticleId);
            if (kbArticle == null)
                throw new NotFoundException("KBArticle", kbArticleId);

            var oldStatus = ticket.Status;
            ticket.Status = TicketStatus.Resolved;
            ticket.ResolvedViaKBArticleId = kbArticleId;
            ticket.FirstRespondedAt ??= DateTime.UtcNow;
            ticket.LastUpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Tickets.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogTicketStatusChangedAsync(
                ticket.Id, oldStatus.ToString(), ticket.Status.ToString(), _currentUser.UserId);

            var notifyUserId = ticket.RaisedForUserId ?? ticket.CreatedByUserId;
            if (notifyUserId > 0)
            {
                var notifyUser = await _unitOfWork.Users.GetByIdAsync(notifyUserId);
                if (notifyUser != null)
                {
                    // FIX: pass oldStatus (captured before mutation), not the new status.
                    await _notificationService.SendStatusChangedAsync(notifyUser, ticket, oldStatus.ToString());
                }
            }

            return true;
        }

        private async Task HandleStatusTransitionAsync(Ticket ticket, TicketStatus oldStatus, TicketStatus newStatus)
        {
            if ((newStatus == TicketStatus.InProgress ||
                 newStatus == TicketStatus.OnHold ||
                 newStatus == TicketStatus.Resolved) &&
                ticket.FirstRespondedAt == null)
            {
                ticket.FirstRespondedAt = DateTime.UtcNow;
            }

            ticket.IsEscalated = false;
            ticket.EscalatedAt = null;
            ticket.EscalationReason = null;
            ticket.EscalationAcknowledgedAt = null;

            if (newStatus == TicketStatus.OnHold)
            {
                ticket.SlaPausedAt = DateTime.UtcNow;
            }
            else if (oldStatus == TicketStatus.OnHold && ticket.SlaPausedAt.HasValue)
            {
                // FIX: use business-hours elapsed so weekends/holidays don't
                // inflate the SLA extension â€” consistent with how deadlines are set.
                var pausedBusinessMinutes = await _slaEngine.CalculateBusinessHoursElapsedAsync(
                    ticket.SlaPausedAt.Value, DateTime.UtcNow) * 60.0;

                ticket.SlaTotalPausedMinutes += pausedBusinessMinutes;
                ticket.SlaPausedAt = null;

                if (ticket.SLADeadline.HasValue)
                    ticket.SLADeadline = ticket.SLADeadline.Value.AddMinutes(pausedBusinessMinutes);
            }

            if (newStatus == TicketStatus.Closed)
            {
                ticket.ClosedWithinSla = !ticket.SLADeadline.HasValue ||
                                         DateTime.UtcNow <= ticket.SLADeadline.Value;
            }
        }

        
        private async Task HandlePostUpdateNotificationsAsync(
            Ticket ticket, TicketStatus oldStatus, int? oldAgentId)
        {
            if (oldStatus != ticket.Status)
            {
                await _auditLogService.LogTicketStatusChangedAsync(
                    ticket.Id, oldStatus.ToString(), ticket.Status.ToString(), _currentUser.UserId);

                var notifyUserId = ticket.RaisedForUserId ?? ticket.CreatedByUserId;
                if (notifyUserId > 0)
                {
                    var notifyUser = await _unitOfWork.Users.GetByIdAsync(notifyUserId);
                    if (notifyUser != null)
                    {
                        if (ticket.Status == TicketStatus.Closed)
                        {
                            var resolutionSummary = ticket.ResolutionSummary ?? "No resolution provided.";
                            await _notificationService.SendTicketClosedAsync(notifyUser, ticket, resolutionSummary);
                        }
                        else if (ticket.Status == TicketStatus.Reopened)
                        {
                            var reopenActor = !string.IsNullOrEmpty(_currentUser.FullName) ? _currentUser.FullName : (_currentUser.Email ?? "User/Agent");
                            var reason = $"Reopened by {reopenActor}";

                            var adminEmail = _configuration["AdminSettings:Email"];
                            if (!string.IsNullOrEmpty(adminEmail))
                            {
                                var adminUser = await _unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.Email == adminEmail);
                                if (adminUser != null)
                                {
                                    await _notificationService.SendTicketReopenedAsync(adminUser, ticket, reason);
                                }
                                else
                                {
                                    var fallbackAdmin = new User { Email = adminEmail, FirstName = "Admin" };
                                    await _notificationService.SendTicketReopenedAsync(fallbackAdmin, ticket, reason);
                                }
                            }
                            if (ticket.AssignedToAgentId.HasValue)
                            {
                                var assignedAgent = await _unitOfWork.Users.GetByIdAsync(ticket.AssignedToAgentId.Value);
                                if (assignedAgent != null)
                                    await _notificationService.SendTicketReopenedAsync(assignedAgent, ticket, reason);
                            }
                            await _notificationService.SendStatusChangedAsync(notifyUser, ticket, oldStatus.ToString());
                        }
                        else
                        {
                            await _notificationService.SendStatusChangedAsync(notifyUser, ticket, oldStatus.ToString());
                        }
                    }
                }
            }

            if (oldAgentId != ticket.AssignedToAgentId)
            {
                await _auditLogService.LogAsync(
                    AuditEventType.TicketAssigned,
                    AuditEntityType.Ticket,
                    ticket.Id,
                    "Ticket assignment changed.",
                    _currentUser.UserId,
                    ticket.Id,
                    new List<AuditLogDetail>
                    {
                        new AuditLogDetail { FieldName = "AssignedToAgentId", OldValue = oldAgentId?.ToString(), NewValue = ticket.AssignedToAgentId?.ToString() }
                    });

                var notifyUserId = ticket.RaisedForUserId ?? ticket.CreatedByUserId;
                User notifyUser = null;
                if (notifyUserId > 0) notifyUser = await _unitOfWork.Users.GetByIdAsync(notifyUserId);

                if (oldAgentId.HasValue && ticket.AssignedToAgentId.HasValue)
                {
                    // Reassignment
                    var oldAgent = await _unitOfWork.Users.GetByIdAsync(oldAgentId.Value);
                    var newAgent = await _unitOfWork.Users.GetByIdAsync(ticket.AssignedToAgentId.Value);
                    if (oldAgent != null && newAgent != null)
                    {
                        await _notificationService.SendAgentReassignedAsync(oldAgent, ticket, oldAgent, newAgent);
                        await _notificationService.SendAgentReassignedAsync(newAgent, ticket, oldAgent, newAgent);
                        if (notifyUser != null)
                            await _notificationService.SendAgentReassignedAsync(notifyUser, ticket, oldAgent, newAgent);
                    }
                }
                else if (!oldAgentId.HasValue && ticket.AssignedToAgentId.HasValue)
                {
                    // First assignment
                    var newAgent = await _unitOfWork.Users.GetByIdAsync(ticket.AssignedToAgentId.Value);
                    if (newAgent != null)
                    {
                        await _notificationService.SendAssignmentAsync(newAgent, ticket);
                        if (notifyUser != null)
                            await _notificationService.SendAssignmentAsync(notifyUser, ticket);
                    }
                }
            }

            await HandleSurveyDispatchAsync(ticket);
        }

        
        private async Task HandleSurveyDispatchAsync(Ticket ticket)
        {
            if ((ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.Closed)
                || ticket.IsSurveySent)
                return;

            var requesterUserId = ticket.RaisedForUserId ?? ticket.CreatedByUserId;
            if (requesterUserId <= 0)
                return;

            var requester = await _unitOfWork.Users.GetByIdAsync(requesterUserId);
            if (requester == null || string.IsNullOrEmpty(requester.Email))
                return;

            // Always mark survey as sent so the in-app survey link appears on ticket detail
            ticket.IsSurveySent = true;
            await _unitOfWork.Tickets.UpdateAsync(ticket);

            // Only send the survey email if the user has NOT opted out
            if (!requester.NotificationPreferences.OptOutSurveys)
            {
                var surveyUrl = $"{_configuration["AppSettings:FrontendUrl"]}/survey/{ticket.Id}";
                await _notificationService.SendSurveyRequestAsync(requester, ticket, surveyUrl);
            }
        }

        private async Task RecalculateSlaInternalAsync(Ticket ticket, string reason)
        {
            ticket.SLADeadline = await _slaEngine.CalculateDeadlineAsync(DateTime.UtcNow, ticket.Priority);
        }

        private async Task<int> ResolveDepartmentIdAsync(int? raisedForUserId, int createdByUserId)
        {
            if (raisedForUserId.HasValue)
            {
                var raisedFor = await _unitOfWork.Users.GetByIdAsync(raisedForUserId.Value);
                if (raisedFor?.DepartmentId.HasValue == true)
                    return raisedFor.DepartmentId.Value;
            }

            var creator = await _unitOfWork.Users.GetByIdAsync(createdByUserId);
            if (creator?.DepartmentId.HasValue == true)
                return creator.DepartmentId.Value;

            var generalDept = await _unitOfWork.Departments.Query()
                .FirstOrDefaultAsync(d => d.Name == "General" && d.IsActive);

            return generalDept?.Id ?? 0;
        }

        private async Task PopulateSlaResponseAsync(Ticket ticket, TicketResponseDto response)
        {
            if (!ticket.SLADeadline.HasValue)
            {
                response.SlaStatus = "Not Set";
                return;
            }

            var now = DateTime.UtcNow;
            response.SlaElapsedHours = Math.Round(
                await _slaEngine.CalculateBusinessHoursElapsedAsync(ticket.CreatedAt, now), 2);

            if (ticket.Status == TicketStatus.Closed || ticket.Status == TicketStatus.Resolved)
            {
                response.SlaStatus = ticket.ClosedWithinSla == false ? "Breached" : "Within SLA";
                return;
            }

            if (now > ticket.SLADeadline.Value)
            {
                response.SlaStatus = "Breached";
                return;
            }

            var totalTargetHours = await _slaEngine.GetResolutionTargetHoursAsync(ticket.Priority);
            response.SlaStatus = response.SlaElapsedHours >= totalTargetHours * 0.75 ? "Warning" : "Within SLA";
        }
    }
}

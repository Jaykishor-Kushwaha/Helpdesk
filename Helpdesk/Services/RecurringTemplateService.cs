using AutoMapper;
using Cronos;
using Helpdesk.DTOs;
using Helpdesk.Enums;
using Helpdesk.Exceptions;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Services
{
    public class RecurringTemplateService : IRecurringTemplateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;
        private readonly ISlaCalculationEngine _slaEngine;

        public RecurringTemplateService(
            IUnitOfWork unitOfWork, 
            IMapper mapper,
            ICurrentUserService currentUser,
            ISlaCalculationEngine slaEngine)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
            _slaEngine = slaEngine;
        }

        public async Task<PagedResponse<RecurringTemplateDto>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var query = _unitOfWork.RecurringTemplates.Query()
                .Include(r => r.Category)
                .Include(r => r.AssignToAgent)
                .Include(r => r.RaiseOnBehalfOf);

            var totalItems = await query.CountAsync();
            var templates = await query
                .OrderBy(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<RecurringTemplateDto>>(templates);

            return new PagedResponse<RecurringTemplateDto>
            {
                Data = dtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }

        public async Task<RecurringTemplateDto?> GetByIdAsync(int id)
        {
            var template = await _unitOfWork.RecurringTemplates.Query()
                .Include(r => r.Category)
                .Include(r => r.AssignToAgent)
                .Include(r => r.RaiseOnBehalfOf)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (template == null) return null;

            return _mapper.Map<RecurringTemplateDto>(template);
        }

        public async Task<RecurringTemplateDto> CreateAsync(CreateRecurringTemplateDto dto)
        {
            await ValidateTemplateReferencesAsync(dto.CategoryId, dto.AssignToAgentId, dto.RaiseOnBehalfOfId);

            // Validate Cron expression
            try
            {
                CronExpression.Parse(dto.CronExpression);
            }
            catch (CronFormatException)
            {
                throw new InvalidOperationException("Invalid Cron expression.");
            }

            var template = _mapper.Map<RecurringTemplate>(dto);
            template.IsActive = true;

            await _unitOfWork.RecurringTemplates.AddAsync(template);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(template.Id) ?? throw new InvalidOperationException("Failed to retrieve created template.");
        }

        public async Task<RecurringTemplateDto?> UpdateAsync(int id, UpdateRecurringTemplateDto dto)
        {
            var template = await _unitOfWork.RecurringTemplates.GetByIdAsync(id);
            if (template == null) throw new NotFoundException("RecurringTemplate", id);

            if (!string.IsNullOrWhiteSpace(dto.CronExpression))
            {
                try
                {
                    CronExpression.Parse(dto.CronExpression);
                    template.CronExpression = dto.CronExpression;
                }
                catch (CronFormatException)
                {
                    throw new InvalidOperationException("Invalid Cron expression.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Name)) template.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.TicketTitle)) template.TicketTitle = dto.TicketTitle;
            if (!string.IsNullOrWhiteSpace(dto.Description)) template.Description = dto.Description;
            if (dto.CategoryId.HasValue) template.CategoryId = dto.CategoryId.Value;
            if (dto.Priority.HasValue) template.Priority = dto.Priority.Value;
            if (dto.AssignToAgentId.HasValue) template.AssignToAgentId = dto.AssignToAgentId.Value;
            if (dto.RaiseOnBehalfOfId.HasValue) template.RaiseOnBehalfOfId = dto.RaiseOnBehalfOfId.Value;
            if (dto.StartDate.HasValue) template.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) template.EndDate = dto.EndDate.Value;
            if (dto.MaxOccurrences.HasValue) template.MaxOccurrences = dto.MaxOccurrences.Value;
            if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;

            await ValidateTemplateReferencesAsync(template.CategoryId, template.AssignToAgentId, template.RaiseOnBehalfOfId);

            await _unitOfWork.RecurringTemplates.UpdateAsync(template);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(template.Id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var template = await _unitOfWork.RecurringTemplates.GetByIdAsync(id);
            if (template == null) return false;

            await _unitOfWork.RecurringTemplates.DeleteAsync(template);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> TriggerNowAsync(int id)
        {
            var template = await _unitOfWork.RecurringTemplates.GetByIdAsync(id);
            if (template == null) throw new NotFoundException("RecurringTemplate", id);

            if (!template.IsActive)
                throw new InvalidOperationException("Cannot manually trigger an inactive template.");

            if (template.MaxOccurrences.HasValue && template.RunCount >= template.MaxOccurrences.Value)
                throw new InvalidOperationException("Template has reached its maximum occurrence count.");

            await ValidateTemplateReferencesAsync(template.CategoryId, template.AssignToAgentId, template.RaiseOnBehalfOfId);

            var now = DateTime.UtcNow;

            var ticket = new Ticket
            {
                Title = template.TicketTitle,
                Description = template.Description,
                Status = TicketStatus.Open,
                Priority = template.Priority,
                CategoryId = template.CategoryId,
                AssignedToAgentId = template.AssignToAgentId,
                RaisedForUserId = template.RaiseOnBehalfOfId,
                CreatedByUserId = _currentUser.UserId,
                CreatedAt = now,
                LastUpdatedAt = now
            };

            // Resolve DepartmentId
            int? deptId = null;
            if (ticket.RaisedForUserId.HasValue)
            {
                var raisedFor = await _unitOfWork.Users.GetByIdAsync(ticket.RaisedForUserId.Value);
                if (raisedFor?.DepartmentId.HasValue == true) deptId = raisedFor.DepartmentId.Value;
            }
            if (!deptId.HasValue)
            {
                var creator = await _unitOfWork.Users.GetByIdAsync(ticket.CreatedByUserId);
                if (creator?.DepartmentId.HasValue == true) deptId = creator.DepartmentId.Value;
            }
            if (!deptId.HasValue)
            {
                var generalDept = await _unitOfWork.Departments.Query().FirstOrDefaultAsync(d => d.Name == "General" && d.IsActive);
                if (generalDept != null) deptId = generalDept.Id;
            }

            if (!deptId.HasValue || deptId <= 0)
                throw new InvalidOperationException("A valid active Department could not be resolved for the generated ticket.");

            ticket.DepartmentId = deptId.Value;
            ticket.SLADeadline = await _slaEngine.CalculateDeadlineAsync(now, ticket.Priority);

            await _unitOfWork.Tickets.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync(); // Get Ticket ID

            var runLog = new RecurringTemplateRunLog
            {
                RecurringTemplateId = template.Id,
                TemplateName = template.Name,
                GeneratedTicketId = ticket.Id,
                ScheduledFireTime = now, // Manual trigger
                ActualFireTime = now
            };
            
            await _unitOfWork.RecurringTemplateRunLogs.AddAsync(runLog);

            template.LastRunAt = now;
            template.RunCount++;
            if (template.MaxOccurrences.HasValue && template.RunCount >= template.MaxOccurrences.Value)
                template.IsActive = false;
            await _unitOfWork.RecurringTemplates.UpdateAsync(template);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private async Task ValidateTemplateReferencesAsync(int categoryId, int? agentId, int? raiseOnBehalfOfId)
        {
            var categoryIsValid = await _unitOfWork.Categories.Query().AnyAsync(c => c.Id == categoryId && c.IsActive);
            if (!categoryIsValid)
                throw new InvalidOperationException("Recurring template category must be active.");

            if (agentId.HasValue)
            {
                var agent = await _unitOfWork.Users.GetByIdAsync(agentId.Value);
                if (agent == null || !agent.IsActive)
                    throw new InvalidOperationException("Assigned agent must be active.");
            }

            if (raiseOnBehalfOfId.HasValue)
            {
                var requester = await _unitOfWork.Users.GetByIdAsync(raiseOnBehalfOfId.Value);
                if (requester == null || !requester.IsActive)
                    throw new InvalidOperationException("Raise-on-behalf-of user must be active.");
            }
        }
    }
}

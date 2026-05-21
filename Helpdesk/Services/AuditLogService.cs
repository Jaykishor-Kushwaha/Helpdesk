using AutoMapper;
using Helpdesk.DTOs;
using Helpdesk.Enums;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Helpdesk.Services
{
    public partial class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            AuditEventType eventType,
            AuditEntityType entityType,
            int entityId,
            string description,
            int performedByUserId,
            int? ticketId = null,
            List<AuditLogDetail>? details = null)
        {
            var auditLog = new AuditLog
            {
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                PerformedByUserId = performedByUserId,
                TicketId = ticketId,
                CreatedAt = DateTime.UtcNow,
                IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                AuditLogDetails = details ?? new List<AuditLogDetail>()
            };

            await _unitOfWork.AuditLogs.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLogResponseDto>> GetAuditLogsAsync()
        {
            var logs = await _unitOfWork.AuditLogs
                .Query()
                .Include(a => a.PerformedByUser)
                .Include(a => a.AuditLogDetails)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AuditLogResponseDto>>(logs);
        }

        public async Task<IEnumerable<AuditLogResponseDto>> SearchAuditLogsAsync(DateTime? startDate, DateTime? endDate, int? actorUserId, AuditEventType? eventType, AuditEntityType? entityType)
        {
            var query = BuildAuditQuery(startDate, endDate, actorUserId, eventType, entityType)
                .Include(a => a.PerformedByUser)
                .Include(a => a.AuditLogDetails);

            var logs = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
            return _mapper.Map<IEnumerable<AuditLogResponseDto>>(logs);
        }

        public async Task<IEnumerable<AuditLogResponseDto>> GetAuditLogsByTicketIdAsync(GetAuditLogsByTicketDto dto)
        {
            var logs = await _unitOfWork.AuditLogs
                .Query()
                .Where(a => a.TicketId == dto.TicketId)
                .Include(a => a.PerformedByUser)
                .Include(a => a.AuditLogDetails)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AuditLogResponseDto>>(logs);
        }

        public async Task<IEnumerable<AuditLogResponseDto>> GetAuditLogsByUserIdAsync(GetAuditLogsByUserDto dto)
        {
            var logs = await _unitOfWork.AuditLogs
                .Query()
                .Where(a => a.PerformedByUserId == dto.UserId)
                .Include(a => a.PerformedByUser)
                .Include(a => a.AuditLogDetails)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AuditLogResponseDto>>(logs);
        }

        public async Task<IEnumerable<AuditLogResponseDto>> GetAuditLogsByEventTypeAsync(AuditEventType eventType)
        {
            var logs = await _unitOfWork.AuditLogs
                .Query()
                .Where(a => a.EventType == eventType)
                .Include(a => a.PerformedByUser)
                .Include(a => a.AuditLogDetails)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AuditLogResponseDto>>(logs);
        }

        public async Task<byte[]> ExportAuditLogsAsync(DateTime? startDate, DateTime? endDate, int? actorUserId = null, AuditEventType? eventType = null, AuditEntityType? entityType = null)
        {
            var query = BuildAuditQuery(startDate, endDate, actorUserId, eventType, entityType)
                .Include(a => a.PerformedByUser);

            var logs = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,EventType,EntityType,EntityId,Description,PerformedBy,TicketId,IpAddress,CreatedAt");

            foreach (var log in logs)
            {
                var user = log.PerformedByUser != null ? $"{log.PerformedByUser.FirstName} {log.PerformedByUser.LastName}" : "System";
                var description = log.Description?.Replace("\"", "\"\"");
                
                sb.AppendLine($"{log.Id},{log.EventType},{log.EntityType},{log.EntityId},\"{description}\",\"{user}\",{log.TicketId},{log.IpAddress},{log.CreatedAt:O}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private IQueryable<AuditLog> BuildAuditQuery(DateTime? startDate, DateTime? endDate, int? actorUserId, AuditEventType? eventType, AuditEntityType? entityType)
        {
            var query = _unitOfWork.AuditLogs.Query().AsQueryable();

            if (startDate.HasValue) query = query.Where(a => a.CreatedAt >= startDate.Value);
            if (endDate.HasValue) query = query.Where(a => a.CreatedAt <= endDate.Value);
            if (actorUserId.HasValue) query = query.Where(a => a.PerformedByUserId == actorUserId.Value);
            if (eventType.HasValue) query = query.Where(a => a.EventType == eventType.Value);
            if (entityType.HasValue) query = query.Where(a => a.EntityType == entityType.Value);

            return query;
        }
    }
}

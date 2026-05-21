using Helpdesk.DTOs;
using Helpdesk.Enums;
using Helpdesk.Models;

namespace Helpdesk.Interfaces
{
    public interface IAuditLogService
    {
        // Old Generic Method (keeping for backwards compatibility during migration)
        Task LogAsync(
            AuditEventType eventType,
            AuditEntityType entityType,
            int entityId,
            string description,
            int performedByUserId,
            int? ticketId = null,
            List<AuditLogDetail>? details = null);

        // Gap 4 Typed Methods
        Task LogTicketCreatedAsync(int ticketId, string description, int performedByUserId);
        Task LogTicketStatusChangedAsync(int ticketId, string oldStatus, string newStatus, int performedByUserId);
        Task LogTicketEscalatedAsync(int ticketId, string reason, int performedByUserId);
        Task LogTicketArchivedAsync(int ticketId, int performedByUserId);
        Task LogSlaOverriddenAsync(int ticketId, string oldDeadline, string newDeadline, string reason, int performedByUserId);
        Task LogArticleCreatedAsync(int articleId, string title, int performedByUserId);
        Task LogArticleUpdatedAsync(int articleId, string title, string status, int performedByUserId);
        Task LogArticleDeletedAsync(int articleId, string title, int performedByUserId);
        Task LogArticlePublishedAsync(int articleId, string title, int performedByUserId);
        Task LogArticleRejectedAsync(int articleId, string title, string reason, int performedByUserId);
        Task LogCommentAddedAsync(int ticketId, int commentId, string content, int performedByUserId);
        Task LogCommentDeletedAsync(int ticketId, int commentId, string content, int performedByUserId);
        Task LogUserChangedAsync(int userId, string description, int performedByUserId, List<AuditLogDetail> details);
        Task LogSystemSettingsChangedAsync(string description, int performedByUserId, List<AuditLogDetail> details);

        Task<IEnumerable<AuditLogResponseDto>> GetAuditLogsAsync();

        Task<IEnumerable<AuditLogResponseDto>> SearchAuditLogsAsync(DateTime? startDate, DateTime? endDate, int? actorUserId, AuditEventType? eventType, AuditEntityType? entityType);

        Task<IEnumerable<AuditLogResponseDto>> GetAuditLogsByTicketIdAsync(GetAuditLogsByTicketDto dto);

        Task<IEnumerable<AuditLogResponseDto>> GetAuditLogsByUserIdAsync(GetAuditLogsByUserDto dto);

        Task<IEnumerable<AuditLogResponseDto>> GetAuditLogsByEventTypeAsync(AuditEventType eventType);
        
        Task<byte[]> ExportAuditLogsAsync(DateTime? startDate, DateTime? endDate, int? actorUserId = null, AuditEventType? eventType = null, AuditEntityType? entityType = null);
    }
}

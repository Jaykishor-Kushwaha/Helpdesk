using Helpdesk.Enums;
using Helpdesk.Models;

namespace Helpdesk.Services
{
    public partial class AuditLogService
    {
        public async Task LogTicketCreatedAsync(int ticketId, string description, int performedByUserId)
        {
            await LogAsync(AuditEventType.TicketCreated, AuditEntityType.Ticket, ticketId, description, performedByUserId, ticketId);
        }

        public async Task LogTicketStatusChangedAsync(int ticketId, string oldStatus, string newStatus, int performedByUserId)
        {
            var details = new List<AuditLogDetail>
            {
                new AuditLogDetail { FieldName = "Status", OldValue = oldStatus, NewValue = newStatus }
            };
            await LogAsync(AuditEventType.TicketStatusChanged, AuditEntityType.Ticket, ticketId, $"Status changed from {oldStatus} to {newStatus}", performedByUserId, ticketId, details);
        }

        public async Task LogTicketEscalatedAsync(int ticketId, string reason, int performedByUserId)
        {
            var details = new List<AuditLogDetail>
            {
                new AuditLogDetail { FieldName = "EscalationReason", OldValue = null, NewValue = reason }
            };
            await LogAsync(AuditEventType.TicketEscalated, AuditEntityType.Ticket, ticketId, $"Ticket escalated. Reason: {reason}", performedByUserId, ticketId, details);
        }

        public async Task LogTicketArchivedAsync(int ticketId, int performedByUserId)
        {
            var details = new List<AuditLogDetail>
            {
                new AuditLogDetail { FieldName = "IsArchived", OldValue = "False", NewValue = "True" }
            };
            await LogAsync(AuditEventType.TicketDeleted, AuditEntityType.Ticket, ticketId, "Ticket archived.", performedByUserId, ticketId, details);
        }

        public async Task LogSlaOverriddenAsync(int ticketId, string oldDeadline, string newDeadline, string reason, int performedByUserId)
        {
            var details = new List<AuditLogDetail>
            {
                new AuditLogDetail { FieldName = "SlaDeadline", OldValue = oldDeadline, NewValue = newDeadline },
                new AuditLogDetail { FieldName = "SlaOverrideReason", OldValue = null, NewValue = reason }
            };
            await LogAsync(AuditEventType.SlaDeadlineOverridden, AuditEntityType.Ticket, ticketId, $"SLA Deadline overridden. Reason: {reason}", performedByUserId, ticketId, details);
        }

        public async Task LogArticleCreatedAsync(int articleId, string title, int performedByUserId)
        {
            await LogAsync(AuditEventType.KBArticleCreated, AuditEntityType.KBArticle, articleId, $"KB Article created: {title}", performedByUserId);
        }

        public async Task LogArticleUpdatedAsync(int articleId, string title, string status, int performedByUserId)
        {
            var details = new List<AuditLogDetail>
            {
                new AuditLogDetail { FieldName = "Title", OldValue = null, NewValue = title },
                new AuditLogDetail { FieldName = "Status", OldValue = null, NewValue = status }
            };
            await LogAsync(AuditEventType.KBArticleUpdated, AuditEntityType.KBArticle, articleId, $"KB Article updated: {title}", performedByUserId, null, details);
        }

        public async Task LogArticleDeletedAsync(int articleId, string title, int performedByUserId)
        {
            await LogAsync(AuditEventType.KBArticleDeleted, AuditEntityType.KBArticle, articleId, $"KB Article deleted: {title}", performedByUserId);
        }

        public async Task LogArticlePublishedAsync(int articleId, string title, int performedByUserId)
        {
            var details = new List<AuditLogDetail>
            {
                new AuditLogDetail { FieldName = "Status", OldValue = "Draft/Pending", NewValue = "Published" }
            };
            await LogAsync(AuditEventType.KBArticlePublished, AuditEntityType.KBArticle, articleId, $"KB Article published: {title}", performedByUserId, null, details);
        }

        public async Task LogArticleRejectedAsync(int articleId, string title, string reason, int performedByUserId)
        {
            var details = new List<AuditLogDetail>
            {
                new AuditLogDetail { FieldName = "Status", OldValue = "Pending", NewValue = "Rejected" },
                new AuditLogDetail { FieldName = "RejectionReason", OldValue = null, NewValue = reason }
            };
            await LogAsync(AuditEventType.KBArticleRejected, AuditEntityType.KBArticle, articleId, $"KB Article rejected: {title}. Reason: {reason}", performedByUserId, null, details);
        }

        public async Task LogCommentAddedAsync(int ticketId, int commentId, string content, int performedByUserId)
        {
            var details = new List<AuditLogDetail>
            {
                new AuditLogDetail { FieldName = "Content", OldValue = null, NewValue = content }
            };
            await LogAsync(AuditEventType.CommentAdded, AuditEntityType.Comment, commentId, $"Comment added to Ticket #{ticketId}", performedByUserId, ticketId, details);
        }

        public async Task LogCommentDeletedAsync(int ticketId, int commentId, string content, int performedByUserId)
        {
            var details = new List<AuditLogDetail>
            {
                new AuditLogDetail { FieldName = "Content", OldValue = content, NewValue = null }
            };
            await LogAsync(AuditEventType.CommentDeleted, AuditEntityType.Comment, commentId, "Comment deleted", performedByUserId, ticketId, details);
        }

        public async Task LogUserChangedAsync(int userId, string description, int performedByUserId, List<AuditLogDetail> details)
        {
            await LogAsync(AuditEventType.UserAccountChanged, AuditEntityType.User, userId, description, performedByUserId, null, details);
        }

        public async Task LogSystemSettingsChangedAsync(string description, int performedByUserId, List<AuditLogDetail> details)
        {
            await LogAsync(AuditEventType.SystemSettingChanged, AuditEntityType.SystemSetting, 0, description, performedByUserId, null, details);
        }
    }
}

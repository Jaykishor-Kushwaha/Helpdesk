using Helpdesk.Models;
namespace Helpdesk.Interfaces
{
    public interface IUnitOfWork
    {
        IGenericRepository<Ticket> Tickets { get; }
        IGenericRepository<User> Users { get; }
        IGenericRepository<Category> Categories { get; }
        IGenericRepository<Department> Departments { get; }
        IGenericRepository<Comment> Comments { get; }
        IGenericRepository<SurveyResponse> Surveys { get; }
        IGenericRepository<AuditLog> AuditLogs { get; }
        IGenericRepository<AuditLogDetail> AuditLogDetails { get; }
        IGenericRepository<SystemSetting> SystemSettings { get; }
        IGenericRepository<NotificationOutbox> NotificationOutboxes { get; }
        IGenericRepository<RecurringTemplate> RecurringTemplates { get; }
        IGenericRepository<KBArticle> KBArticles { get; }
        IGenericRepository<KBArticleVersion> KBArticleVersions { get; }
        IGenericRepository<KBSolveEvent> KBSolveEvents { get; }
        IGenericRepository<KBCommentAttachment> KBCommentAttachments { get; }
        IGenericRepository<RecurringTemplateRunLog> RecurringTemplateRunLogs { get; }
        IGenericRepository<InAppNotification> InAppNotifications { get; }
        Task<int> SaveChangesAsync();
    }
}
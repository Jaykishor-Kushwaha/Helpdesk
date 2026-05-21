using Helpdesk.Data;
using Helpdesk.Interfaces;
using Helpdesk.Models;
namespace Helpdesk.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IGenericRepository<Ticket> Tickets { get; }
        public IGenericRepository<User> Users { get; }
        public IGenericRepository<Category> Categories { get; }
        public IGenericRepository<Department> Departments { get; }
        public IGenericRepository<Comment> Comments { get; }
        public IGenericRepository<SurveyResponse> Surveys { get; }
        public IGenericRepository<AuditLog> AuditLogs { get; }
        public IGenericRepository<AuditLogDetail> AuditLogDetails { get; }

        public IGenericRepository<SystemSetting> SystemSettings =>
            _systemSettings ??= new GenericRepository<SystemSetting>(_context);
        private IGenericRepository<SystemSetting>? _systemSettings;

        public IGenericRepository<NotificationOutbox> NotificationOutboxes =>
            _notificationOutboxes ??= new GenericRepository<NotificationOutbox>(_context);
        private IGenericRepository<NotificationOutbox>? _notificationOutboxes;

        public IGenericRepository<RecurringTemplate> RecurringTemplates =>
            _recurringTemplates ??= new GenericRepository<RecurringTemplate>(_context);
        private IGenericRepository<RecurringTemplate>? _recurringTemplates;

        public IGenericRepository<KBArticle> KBArticles =>
            _kbArticles ??= new GenericRepository<KBArticle>(_context);
        private IGenericRepository<KBArticle>? _kbArticles;

        public IGenericRepository<KBArticleVersion> KBArticleVersions =>
            _kbArticleVersions ??= new GenericRepository<KBArticleVersion>(_context);
        private IGenericRepository<KBArticleVersion>? _kbArticleVersions;

        public IGenericRepository<RecurringTemplateRunLog> RecurringTemplateRunLogs =>
            _recurringTemplateRunLogs ??= new GenericRepository<RecurringTemplateRunLog>(_context);
        private IGenericRepository<RecurringTemplateRunLog>? _recurringTemplateRunLogs;

        public IGenericRepository<KBSolveEvent> KBSolveEvents =>
            _kbSolveEvents ??= new GenericRepository<KBSolveEvent>(_context);
        private IGenericRepository<KBSolveEvent>? _kbSolveEvents;

        public IGenericRepository<KBCommentAttachment> KBCommentAttachments =>
            _kbCommentAttachments ??= new GenericRepository<KBCommentAttachment>(_context);
        private IGenericRepository<KBCommentAttachment>? _kbCommentAttachments;

        public IGenericRepository<InAppNotification> InAppNotifications =>
            _inAppNotifications ??= new GenericRepository<InAppNotification>(_context);
        private IGenericRepository<InAppNotification>? _inAppNotifications;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Tickets = new GenericRepository<Ticket>(context);
            Users = new GenericRepository<User>(context);
            Categories = new GenericRepository<Category>(context);
            Departments = new GenericRepository<Department>(context);
            Comments = new GenericRepository<Comment>(context);
            Surveys = new GenericRepository<SurveyResponse>(context);
            AuditLogs = new GenericRepository<AuditLog>(context);
            AuditLogDetails = new GenericRepository<AuditLogDetail>(context);
        }

        public async Task<int> SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
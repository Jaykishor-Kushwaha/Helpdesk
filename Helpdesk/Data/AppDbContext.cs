using Helpdesk.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int,
        IdentityUserClaim<int>,
        AppUserRole,
        IdentityUserLogin<int>,
        IdentityRoleClaim<int>,
        IdentityUserToken<int>>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<AuditLogDetail> AuditLogDetails => Set<AuditLogDetail>();

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<KBArticle> KBArticles => Set<KBArticle>();
        public DbSet<KBArticleVersion> KBArticleVersions => Set<KBArticleVersion>();
        public DbSet<KBSolveEvent> KBSolveEvents => Set<KBSolveEvent>();
        public DbSet<KBCommentAttachment> KBCommentAttachments => Set<KBCommentAttachment>();
        public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
        public DbSet<RecurringTemplate> RecurringTemplates => Set<RecurringTemplate>();
        public DbSet<RecurringTemplateRunLog> RecurringTemplateRunLogs => Set<RecurringTemplateRunLog>();
        public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();
        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
        public DbSet<InAppNotification> InAppNotifications => Set<InAppNotification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ AppUserRole - fix shadow foreign key warnings
            modelBuilder.Entity<AppUserRole>(userRole =>
            {
                userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

                userRole.HasOne(ur => ur.Role)
                    .WithMany()
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                userRole.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            // Ticket
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.CreatedByUser)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.RaisedForUser)
                .WithMany(u => u.RaisedForTickets)
                .HasForeignKey(t => t.RaisedForUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.AssignedToAgent)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedToAgentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Department)
                .WithMany(d => d.Tickets)
                .HasForeignKey(t => t.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.AuthorUser)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorUserId);

            // AuditLog
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.PerformedByUser)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.PerformedByUserId);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.Ticket)
                .WithMany(t => t.AuditLogs)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            // AuditLog Append-Only Enforcement
            foreach (var property in modelBuilder.Entity<AuditLog>().Metadata.GetProperties())
            {
                property.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
            }

            // AuditLogDetail
            modelBuilder.Entity<AuditLogDetail>()
                .HasOne(a => a.AuditLog)
                .WithMany(al => al.AuditLogDetails)
                .HasForeignKey(a => a.AuditLogId);

            // AuditLogDetail Append-Only Enforcement
            foreach (var property in modelBuilder.Entity<AuditLogDetail>().Metadata.GetProperties())
            {
                property.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
            }

            // RelatedTicket Self-Referencing
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.RelatedTicket)
                .WithMany()
                .HasForeignKey(t => t.RelatedTicketId)
                .OnDelete(DeleteBehavior.Restrict);

            // ResolvedViaKB Link
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.ResolvedViaKBArticle)
                .WithMany()
                .HasForeignKey(t => t.ResolvedViaKBArticleId)
                .OnDelete(DeleteBehavior.Restrict);

            // DepartmentHead
            modelBuilder.Entity<Department>()
                .HasOne(d => d.DepartmentHead)
                .WithMany()
                .HasForeignKey(d => d.DepartmentHeadId)
                .OnDelete(DeleteBehavior.Restrict);

            // NotificationPreferences JSON Conversion
            modelBuilder.Entity<User>()
                .Property(u => u.NotificationPreferences)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<NotificationPreferences>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());

            // Prevent cascade loops on KBArticles
            modelBuilder.Entity<KBArticle>()
                .HasOne(k => k.Author)
                .WithMany()
                .HasForeignKey(k => k.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KBArticleVersion>()
                .HasOne(kv => kv.CreatedByUser)
                .WithMany()
                .HasForeignKey(kv => kv.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // KBSolveEvent Link
            modelBuilder.Entity<KBSolveEvent>()
                .HasIndex(x => new { x.KBArticleId, x.UserId, x.SolvedAt });

            modelBuilder.Entity<KBSolveEvent>()
                .HasOne(s => s.Ticket)
                .WithMany()
                .HasForeignKey(s => s.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KBSolveEvent>()
                .HasOne(s => s.KBArticle)
                .WithMany()
                .HasForeignKey(s => s.KBArticleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KBSolveEvent>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // KBCommentAttachment Link & Unique Guard
            modelBuilder.Entity<KBCommentAttachment>()
                .HasIndex(kca => new { kca.CommentId, kca.KBArticleId }).IsUnique();

            modelBuilder.Entity<KBCommentAttachment>()
                .HasOne(kca => kca.Comment)
                .WithMany()
                .HasForeignKey(kca => kca.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<KBCommentAttachment>()
                .HasOne(kca => kca.KBArticle)
                .WithMany()
                .HasForeignKey(kca => kca.KBArticleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KBCommentAttachment>()
                .HasOne(kca => kca.AttachedByUser)
                .WithMany()
                .HasForeignKey(kca => kca.AttachedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // InAppNotification relationships
            modelBuilder.Entity<InAppNotification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InAppNotification>()
                .HasOne(n => n.Ticket)
                .WithMany()
                .HasForeignKey(n => n.TicketId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = ChangeTracker.Entries()
                .Where(e => e.Entity is AuditLog || e.Entity is AuditLogDetail)
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted);

            if (auditEntries.Any())
            {
                throw new InvalidOperationException("AuditLog and AuditLogDetail records are append-only. Updates and Deletes are strictly prohibited.");
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}

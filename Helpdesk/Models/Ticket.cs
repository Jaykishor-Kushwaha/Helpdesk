using Helpdesk.Enums;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class Ticket : IEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        [Required]
        public TicketPriority Priority { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FirstRespondedAt { get; set; }
        public bool? ClosedWithinSla { get; set; }

        // ✅ Required - ticket must have a creator
        public int CreatedByUserId { get; set; }

        // ✅ Nullable - defaults to creator if not provided
        public int? RaisedForUserId { get; set; }

        // ✅ Nullable - assigned later by agent/admin
        public int? AssignedToAgentId { get; set; }

        // ✅ Required - must belong to a category
        public int CategoryId { get; set; }

        // ✅ Navigation properties
        public User CreatedByUser { get; set; } = null!;

        // ✅ Nullable navigation - matches nullable FK
        public User? RaisedForUser { get; set; }

        public User? AssignedToAgent { get; set; }
        public Category Category { get; set; } = null!;


        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
        [MaxLength(100)]
        public string? AffectedAsset { get; set; }
        
        public int? RelatedTicketId { get; set; }
        public Ticket? RelatedTicket { get; set; }

        public int? ResolvedViaKBArticleId { get; set; }
        public KBArticle? ResolvedViaKBArticle { get; set; }

        public DateTime? SLADeadline { get; set; }
        
        public bool IsEscalated { get; set; } = false;
        public bool IsAutoEscalated { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionSummary { get; set; }
        
        [MaxLength(500)]
        public string? EscalationReason { get; set; }
        public DateTime? EscalatedAt { get; set; }
        public DateTime? EscalationAcknowledgedAt { get; set; }
        public TicketStatus? LastEscalatedStatus { get; set; }

        public int ReopenCount { get; set; } = 0;

        public bool SlaWarningSent { get; set; } = false;
        public bool IsSurveySent { get; set; } = false;

        public DateTime? SlaPausedAt { get; set; }
        public double SlaTotalPausedMinutes { get; set; } = 0;

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        // Domain Methods (Rich Model)
        public void BumpPriority()
        {
            if (Priority == TicketPriority.Low) Priority = TicketPriority.Medium;
            else if (Priority == TicketPriority.Medium) Priority = TicketPriority.High;
            else if (Priority == TicketPriority.High) Priority = TicketPriority.Critical;
        }

        public bool CanEscalateInCurrentStatus()
        {
            return !IsEscalated && LastEscalatedStatus != Status;
        }

        public void IncrementReopenCount()
        {
            ReopenCount++;
        }

        public bool HasReachedReopenLimit()
        {
            return ReopenCount >= 3;
        }
    }
}

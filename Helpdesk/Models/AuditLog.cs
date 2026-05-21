using Helpdesk.Enums;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class AuditLog  : IEntity
    {
        public int Id { get; set; }

        public AuditEventType EventType { get; set; }
        public AuditEntityType EntityType { get; set; }
        public int EntityId { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        // Foreign keys
        public int PerformedByUserId { get; set; }
        public int? TicketId { get; set; }

        // Navigation properties
        public User PerformedByUser { get; set; } = null!;
        public Ticket? Ticket { get; set; }

        public ICollection<AuditLogDetail> AuditLogDetails { get; set; } = new List<AuditLogDetail>();

    }
}

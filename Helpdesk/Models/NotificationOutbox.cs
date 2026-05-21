using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class NotificationOutbox : IEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string RecipientEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public Helpdesk.Enums.NotificationStatus Status { get; set; } = Helpdesk.Enums.NotificationStatus.Pending;

        public int RetryCount { get; set; } = 0;
        public DateTime? NextRetryAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

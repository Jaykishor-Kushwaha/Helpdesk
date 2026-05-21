using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class Comment : IEntity
    { 

        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign keys
        public int TicketId { get; set; }
        public int AuthorUserId { get; set; }

        // Navigation properties
        public Ticket Ticket { get; set; } = null!;
        public User AuthorUser { get; set; } = null!;

    }
}

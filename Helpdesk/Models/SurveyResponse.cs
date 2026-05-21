using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class SurveyResponse : IEntity
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;

        public int? SubmittedByUserId { get; set; }
        public User? SubmittedByUser { get; set; }

        [Range(1, 5)]
        public int Score { get; set; }

        [MaxLength(500)]
        public string? Comments { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}

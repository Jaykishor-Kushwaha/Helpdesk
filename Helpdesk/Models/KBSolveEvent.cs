using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class KBSolveEvent : IEntity
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public int KBArticleId { get; set; }
        public KBArticle? KBArticle { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime SolvedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// True if the user dismissed the ticket, False if they indicated it didn't help.
        /// </summary>
        public bool IsHelpful { get; set; } = true;
    }
}

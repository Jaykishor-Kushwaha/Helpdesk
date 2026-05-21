using Helpdesk.Enums;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class RecurringTemplate : IEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string TicketTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public TicketPriority Priority { get; set; }

        public int? AssignToAgentId { get; set; }
        public User? AssignToAgent { get; set; }

        public int? RaiseOnBehalfOfId { get; set; }
        public User? RaiseOnBehalfOf { get; set; }

        [Required]
        public string CronExpression { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxOccurrences { get; set; }
        public int RunCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        
        public DateTime? LastRunAt { get; set; }
    }
}

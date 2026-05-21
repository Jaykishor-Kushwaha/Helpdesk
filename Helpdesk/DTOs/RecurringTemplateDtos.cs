using Helpdesk.Enums;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.DTOs
{
    public class RecurringTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TicketTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int? AssignToAgentId { get; set; }
        public string? AssignToAgentName { get; set; }
        public int? RaiseOnBehalfOfId { get; set; }
        public string? RaiseOnBehalfOfName { get; set; }
        public string CronExpression { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxOccurrences { get; set; }
        public int RunCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastRunAt { get; set; }
    }

    public class CreateRecurringTemplateDto
    {
        [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;
        [Required] [MaxLength(150)] public string TicketTitle { get; set; } = string.Empty;
        [Required] [MaxLength(2000)] public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public TicketPriority Priority { get; set; }
        public int? AssignToAgentId { get; set; }
        public int? RaiseOnBehalfOfId { get; set; }
        [Required] public string CronExpression { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxOccurrences { get; set; }
    }

    public class UpdateRecurringTemplateDto : GetByIdDto
    {
        [MaxLength(100)] public string? Name { get; set; }
        [MaxLength(150)] public string? TicketTitle { get; set; }
        [MaxLength(2000)] public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public TicketPriority? Priority { get; set; }
        public int? AssignToAgentId { get; set; }
        public int? RaiseOnBehalfOfId { get; set; }
        public string? CronExpression { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxOccurrences { get; set; }
        public bool? IsActive { get; set; }
    }
}

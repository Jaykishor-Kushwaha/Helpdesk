using Helpdesk.Enums;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.DTOs
{
    public class ReportFilterDto
    {
        [Required]
        public ReportType ReportType { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int? CategoryId { get; set; }
        public TicketPriority? Priority { get; set; }
        public int? AssignedToAgentId { get; set; }
        public int? DepartmentId { get; set; }
        public string Format { get; set; } = "pdf";
    }
}

using Helpdesk.Enums;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.DTOs
{
    public class TicketFilterDto
    {
        public TicketStatus? Status { get; set; }
        public TicketPriority? Priority { get; set; }
        public int? CategoryId { get; set; }
        public int? AssignedToAgentId { get; set; }
        public int? RaisedByUserId { get; set; }
        public string? SearchTerm { get; set; }

        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        // ✅ THIS FIXES YOUR CURRENT ERRORS
        public string? SortBy { get; set; } = "createdAt";
        public bool Desc { get; set; } = true;
    }
}
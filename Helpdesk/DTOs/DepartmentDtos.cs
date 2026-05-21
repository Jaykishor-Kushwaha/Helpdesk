using System.ComponentModel.DataAnnotations;

namespace Helpdesk.DTOs
{
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? DepartmentHeadId { get; set; }
        public string? DepartmentHeadName { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateDepartmentDto
    {
        [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;
        public int? DepartmentHeadId { get; set; }
    }

    public class UpdateDepartmentDto : GetByIdDto
    {
        [MaxLength(100)] public string? Name { get; set; }
        public int? DepartmentHeadId { get; set; }
        public bool? IsActive { get; set; }
    }

    public class DepartmentSummaryDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int ActiveUserCount { get; set; }
        public int OpenTicketCount { get; set; }
        public int RecentTicketCount { get; set; }
    }
}

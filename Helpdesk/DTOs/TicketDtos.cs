using Helpdesk.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
namespace Helpdesk.DTOs
{
    public class CreateTicketDto
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public TicketPriority Priority { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        public int? RaisedForUserId { get; set; }
        public int? AssignedToAgentId { get; set; }

        [MaxLength(100)]
        public string? AffectedAsset { get; set; }

        public int? RelatedTicketId { get; set; }

        // ✅ Normalize values after deserialization
        public void NormalizeValues()
        {
            // Convert 0 to null for FK fields
            if (CategoryId == 0) CategoryId = 0; // Keep for validation error
            if (DepartmentId == 0) DepartmentId = 0; // Keep for validation error
            if (RaisedForUserId == 0) RaisedForUserId = null;
            if (AssignedToAgentId == 0) AssignedToAgentId = null;
            if (RelatedTicketId == 0) RelatedTicketId = null;
            if (AffectedAsset == "string") AffectedAsset = null; // Handle placeholder
        }
    }

    public class UpdateTicketDto : GetByIdDto
    {
        [MaxLength(150)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public TicketStatus? Status { get; set; }

        public TicketPriority? Priority { get; set; }

        public int? CategoryId { get; set; }
        public int? DepartmentId { get; set; }

        public int? AssignedToAgentId { get; set; }

        [MaxLength(100)]
        public string? AffectedAsset { get; set; }

        public int? RelatedTicketId { get; set; }

        // ✅ POST-DESERIALIZATION VALIDATION: Clean up invalid values
        public void NormalizeValues()
        {
            // Convert string "null" to actual null
            if (Title == "null") Title = null;
            if (Description == "null") Description = null;
            if (AffectedAsset == "null") AffectedAsset = null;

            // Convert 0 to null for FK fields
            if (CategoryId == 0) CategoryId = null;
            if (DepartmentId == 0) DepartmentId = null;
            if (AssignedToAgentId == 0) AssignedToAgentId = null;
            if (RelatedTicketId == 0) RelatedTicketId = null;
        }
    }

    public class TicketResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        // flat strings — simpler API response
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public int? RaisedForUserId { get; set; }
        public string? RaisedForUserName { get; set; }
        public int? AssignedToAgentId { get; set; }
        public string? AssignedToAgentName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        
        public string? AffectedAsset { get; set; }
        public int? RelatedTicketId { get; set; }
        public DateTime? SlaDeadline { get; set; }
        public string SlaStatus { get; set; } = string.Empty;
        public double? SlaElapsedHours { get; set; }
        public DateTime? FirstRespondedAt { get; set; }
        public bool? ClosedWithinSla { get; set; }
        public bool IsEscalated { get; set; }
    }
 

    public class TicketActionDto : GetByIdDto
    {
        public int UserId { get; set; }
        public UserRole Role { get; set; }
    }

    public class GetTicketByIdDto : GetByIdDto
    {
        public int UserId { get; set; }
        public UserRole Role { get; set; }
    }

    //public class CreateTicketWithUserDto : CreateTicketDto  
    //{
    //    public int UserId { get; set; }          
    //}

    public class CreateTicketResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TicketId { get; set; }
    }

    public class EscalateTicketDto
    {
        [Required]
        public int TicketId { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class OverrideSlaDto
    {
        [Required]
        public DateTime NewSlaDeadline { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}

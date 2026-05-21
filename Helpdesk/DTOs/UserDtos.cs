using Helpdesk.Enums;
using System.ComponentModel.DataAnnotations;
namespace Helpdesk.DTOs
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must contain uppercase, lowercase, number and special character")]
        public string? Password { get; set; }
        [Required(ErrorMessage = "Role is required")]
        [RegularExpression("^(Admin|Agent|User)$",
            ErrorMessage = "Role must be Admin, Agent or User")]
        public string Role { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
    }
    public class UpdateUserDto : GetByIdDto
    {
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;
        [RegularExpression("^(Admin|Agent|User)$",
            ErrorMessage = "Role must be Admin, Agent or User")]
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? DepartmentId { get; set; }
    }
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public bool IsActive { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public NotificationPreferencesDto NotificationPreferences { get; set; } = new NotificationPreferencesDto();
    }
    public class UserActionDto : GetByIdDto
    {
        public int UserId { get; set; }
        public UserRole Role { get; set; }
    }

    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? JobTitle { get; set; }
    }

    public class NotificationPreferencesDto
    {
        public bool EmailOnTicketCreated { get; set; }
        public bool EmailOnStatusChange { get; set; }
        public bool EmailOnComment { get; set; }
        public bool EmailOnAssignment { get; set; }
        public bool OptOutSurveys { get; set; }
    }
}

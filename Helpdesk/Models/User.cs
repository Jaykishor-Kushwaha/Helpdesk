using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Models
{
    public class User : IdentityUser<int>, IEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string? JobTitle { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool RequiresPasswordChange { get; set; } = false;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // ✅ Add these for Role navigation
        public ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
        public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();
        public ICollection<Ticket> RaisedForTickets { get; set; } = new List<Ticket>();
        public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }
        
        public NotificationPreferences NotificationPreferences { get; set; } = new();
    }
}
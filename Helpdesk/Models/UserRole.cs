using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Models
{
    public class AppUserRole : IdentityUserRole<int>
    {
        public User User { get; set; } = null!;
        public IdentityRole<int> Role { get; set; } = null!;
    }
}
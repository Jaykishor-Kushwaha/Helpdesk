using System.Security.Claims;
using Helpdesk.Enums;
using Helpdesk.Helper;
using Helpdesk.Interfaces;

namespace Helpdesk.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public int UserId
        {
            get
            {
                var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                 ?? User?.FindFirst("nameid")?.Value
                                 ?? User?.FindFirst("sub")?.Value;

                if (int.TryParse(userIdClaim, out var id))
                    return id;

                throw new UnauthorizedAccessException("User ID not found in token");
            }
        }

        public string Role =>
            User?.FindFirst(ClaimTypes.Role)?.Value
            ?? User?.FindFirst("role")?.Value
            ?? "Guest";

        public UserRole UserRole => Role != "Guest" ? RoleHelper.GetRoleEnum(Role) : UserRole.Guest;

      
        public string FullName => User?.FindFirst(ClaimTypes.Name)?.Value ?? "";

        public string Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? "";

        public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
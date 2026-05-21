 using Helpdesk.Enums;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected readonly ICurrentUserService _currentUser;

        protected BaseController(ICurrentUserService currentUser)
        {
            _currentUser = currentUser;
        }

        // ✅ Modern (preferred)
        protected int CurrentUserId => _currentUser.UserId;
        protected UserRole CurrentUserRole => _currentUser.UserRole;
        protected string CurrentUserRoleName => _currentUser.Role;

        // ✅ Compatibility (so existing code doesn't break)
        protected int GetUserId() => CurrentUserId;
        protected UserRole GetUserRole() => CurrentUserRole;
        protected string GetUserRoleString() => CurrentUserRoleName;
    }
}
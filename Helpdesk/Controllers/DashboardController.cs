using Helpdesk.DTOs;
using Helpdesk.Helper;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [Authorize(Roles = Roles.AdminAndAgent)]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(
            IDashboardService dashboardService,
            ICurrentUserService currentUser) : base(currentUser)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _dashboardService.GetDashboardStatsAsync();
            return Ok(ApiResponse<DashboardDto>.SuccessResponse(stats));
        }
    }
}
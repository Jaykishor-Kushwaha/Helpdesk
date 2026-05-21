using Helpdesk.DTOs;
namespace Helpdesk.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardStatsAsync();
    }
}
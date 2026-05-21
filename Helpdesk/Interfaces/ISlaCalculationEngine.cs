using Helpdesk.Enums;

namespace Helpdesk.Interfaces
{
    public interface ISlaCalculationEngine
    {
        Task<DateTime> CalculateDeadlineAsync(DateTime startTime, TicketPriority priority);
        Task<DateTime> CalculateNextBusinessDayStartAsync(DateTime time);
        Task<double> CalculateBusinessHoursElapsedAsync(DateTime startTime, DateTime endTime);
        Task<double> GetResolutionTargetHoursAsync(TicketPriority priority);
    }
}

using Helpdesk.Enums;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Services
{
    public class SlaCalculationEngine : ISlaCalculationEngine
    {
        private readonly IUnitOfWork _unitOfWork;

        public SlaCalculationEngine(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private async Task<(TimeSpan start, TimeSpan end, List<DayOfWeek> days, List<DateTime> holidays)> LoadSettingsAsync()
        {
            var settings = await _unitOfWork.SystemSettings.Query().ToListAsync();
            
            var start = TimeSpan.Parse(
                settings.FirstOrDefault(s => s.Key == "BusinessHoursStart")?.Value ?? "09:00");
            var end = TimeSpan.Parse(
                settings.FirstOrDefault(s => s.Key == "BusinessHoursEnd")?.Value ?? "17:00");
            var days = (settings.FirstOrDefault(s => s.Key == "WorkingDays")?.Value 
                ?? "Monday,Tuesday,Wednesday,Thursday,Friday")
                .Split(',')
                .Select(d => Enum.Parse<DayOfWeek>(d.Trim()))
                .ToList();
            var holidaysStr = settings.FirstOrDefault(s => s.Key == "PublicHolidays")?.Value ?? "";
            var holidays = string.IsNullOrWhiteSpace(holidaysStr)
                ? new List<DateTime>()
                : holidaysStr.Split(',')
                    .Select(d => DateTime.Parse(d.Trim()))
                    .ToList();

            return (start, end, days, holidays);
        }

        public async Task<double> GetResolutionTargetHoursAsync(TicketPriority priority)
        {
            var settings = await _unitOfWork.SystemSettings.Query().ToListAsync();
            var key = priority switch
            {
                TicketPriority.Critical => "SlaTargetCritical",
                TicketPriority.High => "SlaTargetHigh",
                TicketPriority.Medium => "SlaTargetMedium",
                TicketPriority.Low => "SlaTargetLow",
                _ => "SlaTargetHigh"
            };

            var configured = settings.FirstOrDefault(s => s.Key == key)?.Value;
            if (double.TryParse(configured, out var hours) && hours > 0)
                return hours;

            return priority switch
            {
                TicketPriority.Critical => 4,
                TicketPriority.High => 8,
                TicketPriority.Medium => 24,
                TicketPriority.Low => 40,
                _ => 8
            };
        }

        public async Task<DateTime> CalculateDeadlineAsync(DateTime startTime, TicketPriority priority)
        {
            var (startOfDay, endOfDay, workingDays, holidays) = await LoadSettingsAsync();

            double requiredHours = await GetResolutionTargetHoursAsync(priority);

            DateTime current = AdjustToBusinessHours(startTime, startOfDay, endOfDay, workingDays, holidays);
            double hoursAdded = 0;

            while (hoursAdded < requiredHours)
            {
                DateTime endOfCurrentDay = current.Date + endOfDay;
                double hoursLeftToday = (endOfCurrentDay - current).TotalHours;

                if (hoursAdded + hoursLeftToday >= requiredHours)
                {
                    current = current.AddHours(requiredHours - hoursAdded);
                    break;
                }
                else
                {
                    hoursAdded += hoursLeftToday;
                    current = AdjustToBusinessHours(endOfCurrentDay.AddMinutes(1), startOfDay, endOfDay, workingDays, holidays);
                }
            }

            return current;
        }

        public async Task<DateTime> CalculateNextBusinessDayStartAsync(DateTime time)
        {
            var (startOfDay, endOfDay, workingDays, holidays) = await LoadSettingsAsync();
            return AdjustToBusinessHours(time, startOfDay, endOfDay, workingDays, holidays);
        }

        public async Task<double> CalculateBusinessHoursElapsedAsync(DateTime startTime, DateTime endTime)
        {
            if (endTime <= startTime) return 0;
            var (startOfDay, endOfDay, workingDays, holidays) = await LoadSettingsAsync();
            DateTime current = AdjustToBusinessHours(startTime, startOfDay, endOfDay, workingDays, holidays);
            
            if (current >= endTime) return 0; // If startTime was already after endTime in business hours

            double totalHours = 0;
            
            while (current.Date < endTime.Date)
            {
                totalHours += (endOfDay - current.TimeOfDay).TotalHours;
                current = AdjustToBusinessHours(current.Date.AddDays(1) + startOfDay, startOfDay, endOfDay, workingDays, holidays);
                if (current >= endTime) break;
            }
            
            if (current.Date == endTime.Date && endTime.TimeOfDay > current.TimeOfDay)
            {
                var endTick = endTime.TimeOfDay > endOfDay ? endOfDay : endTime.TimeOfDay;
                if (endTick > current.TimeOfDay)
                    totalHours += (endTick - current.TimeOfDay).TotalHours;
            }
            return totalHours;
        }

        private DateTime AdjustToBusinessHours(DateTime time, TimeSpan startOfDay, TimeSpan endOfDay, List<DayOfWeek> workingDays, List<DateTime> holidays)
        {
            DateTime current = time;

            if (current.TimeOfDay < startOfDay)
                current = current.Date + startOfDay;
            if (current.TimeOfDay >= endOfDay)
                current = current.Date.AddDays(1) + startOfDay;

            int safetyLimit = 365;
            int daysChecked = 0;

            while (!workingDays.Contains(current.DayOfWeek) || holidays.Any(h => h.Date == current.Date))
            {
                if (++daysChecked > safetyLimit)
                    throw new InvalidOperationException("Could not find a valid business day within 365 days. Check WorkingDays and PublicHolidays settings.");
                current = current.Date.AddDays(1) + startOfDay;
            }

            return current;
        }
    }
}

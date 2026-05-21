using Helpdesk.Enums;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Workers
{
    public class TicketArchivalWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TicketArchivalWorker> _logger;

        public TicketArchivalWorker(IServiceScopeFactory scopeFactory, ILogger<TicketArchivalWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ticket Archival Worker started.");

            using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
            
            // Execute once at startup, then every 24 hours
            do
            {
                try
                {
                    await ArchiveOldTicketsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while archiving tickets.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task ArchiveOldTicketsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

            var archivalSetting = await unitOfWork.SystemSettings.Query()
                .FirstOrDefaultAsync(s => s.Key == "ArchivalPolicyMonths", stoppingToken);

            if (!int.TryParse(archivalSetting?.Value, out int monthsToArchive) || monthsToArchive <= 0)
            {
                monthsToArchive = 12; // Default to 12 months if not configured or invalid
            }

            var cutoffDate = DateTime.UtcNow.AddMonths(-monthsToArchive);

            var ticketsToArchive = await unitOfWork.Tickets.Query()
                .Where(t => (t.Status == TicketStatus.Closed || t.Status == TicketStatus.Resolved) 
                            && t.LastUpdatedAt < cutoffDate)
                .ToListAsync(stoppingToken);

            if (!ticketsToArchive.Any())
            {
                _logger.LogInformation("No tickets to archive at this time.");
                return;
            }

            foreach (var ticket in ticketsToArchive)
            {
                ticket.Status = TicketStatus.Archived;
                ticket.LastUpdatedAt = DateTime.UtcNow;
                await unitOfWork.Tickets.UpdateAsync(ticket);
            }

            await unitOfWork.SaveChangesAsync();

            // Bulk log the archival (to avoid thousands of individual audit logs if possible, but PRD standard is per ticket)
            // Or log per ticket since this is a system admin task
            foreach (var ticket in ticketsToArchive)
            {
                await auditLogService.LogAsync(
                    AuditEventType.TicketArchived,
                    AuditEntityType.Ticket,
                    ticket.Id,
                    $"Ticket archived by system policy (>{monthsToArchive} months old).",
                    0, // System User
                    ticket.Id);
            }

            _logger.LogInformation($"Archived {ticketsToArchive.Count} tickets older than {monthsToArchive} months.");
        }
    }
}

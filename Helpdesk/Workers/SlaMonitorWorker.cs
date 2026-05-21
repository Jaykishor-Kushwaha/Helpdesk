using Helpdesk.Enums;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Workers
{
    public class SlaMonitorWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SlaMonitorWorker> _logger;
        private readonly IConfiguration _configuration;

        public SlaMonitorWorker(IServiceProvider serviceProvider, ILogger<SlaMonitorWorker> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SLA Monitor Worker started.");
            
            // Poll every 5 minutes
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessSlaChecksAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing SLA checks.");
                }
            }
        }

        private async Task ProcessSlaChecksAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

            var slaEngine = scope.ServiceProvider.GetRequiredService<ISlaCalculationEngine>();

            var generalDept = await unitOfWork.Departments.Query()
                .Include(d => d.DepartmentHead)
                .FirstOrDefaultAsync(d => d.Name == "General");

            var now = DateTime.UtcNow;

            // 1. Process Warnings (SLA >= 75% elapsed)
            var warningCandidates = await unitOfWork.Tickets.Query()
                .Include(t => t.AssignedToAgent)
                .Where(t => (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
                            && t.SLADeadline > now
                            && !t.SlaWarningSent
                            && !t.IsEscalated
                            && t.SLADeadline.HasValue)
                .ToListAsync(stoppingToken);

            var warningTickets = new List<Ticket>();
            foreach (var t in warningCandidates)
            {
                var totalDurationHours = await slaEngine.GetResolutionTargetHoursAsync(t.Priority);
                var elapsedHours = await slaEngine.CalculateBusinessHoursElapsedAsync(t.CreatedAt, now);
                if (elapsedHours >= totalDurationHours * 0.75)
                {
                    warningTickets.Add(t);
                }
            }

            foreach (var ticket in warningTickets)
            {
                ticket.SlaWarningSent = true;
                await unitOfWork.Tickets.UpdateAsync(ticket);

                if (ticket.AssignedToAgentId.HasValue && ticket.AssignedToAgent != null)
                {
                    await notificationService.SendSlaWarningAsync(ticket.AssignedToAgent, ticket);
                }
                
                var adminEmail = _configuration["AdminSettings:Email"];
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    var adminUser = await unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.Email == adminEmail, stoppingToken);
                    if (adminUser != null)
                    {
                        await notificationService.SendSlaWarningAsync(adminUser, ticket);
                    }
                    else
                    {
                        var fallbackAdmin = new User { Email = adminEmail, FirstName = "Admin" };
                        await notificationService.SendSlaWarningAsync(fallbackAdmin, ticket);
                    }
                }
            }

            // 2. Process Breaches & Escalations (SLA passed)
            var breachedTickets = await unitOfWork.Tickets.Query()
                .Include(t => t.AssignedToAgent)
                .Include(t => t.CreatedByUser)
                .Include(t => t.RaisedForUser)
                .Include(t => t.Department)
                    .ThenInclude(d => d.DepartmentHead)
                .Where(t => (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
                            && t.SLADeadline <= now
                            && !t.IsEscalated)
                .ToListAsync(stoppingToken);

            foreach (var ticket in breachedTickets)
            {
                ticket.IsEscalated = true;
                ticket.EscalatedAt = now;
                ticket.EscalationReason = "Automated SLA Breach";
                ticket.LastEscalatedStatus = ticket.Status;
                ticket.BumpPriority();

                await unitOfWork.Tickets.UpdateAsync(ticket);
                
                var logUserId = ticket.CreatedByUserId;

                await auditLogService.LogAsync(
                    AuditEventType.TicketEscalated,
                    AuditEntityType.Ticket,
                    ticket.Id,
                    "Automated SLA Breach escalation triggered.",
                    logUserId,
                    ticket.Id);

                // Notify Assigned Agent
                if (ticket.AssignedToAgentId.HasValue && ticket.AssignedToAgent != null)
                {
                    await notificationService.SendSlaBreachAsync(ticket.AssignedToAgent, ticket);
                }

                // Notify User
                var raisedFor = ticket.RaisedForUser ?? ticket.CreatedByUser;
                if (raisedFor != null)
                {
                    await notificationService.SendSlaBreachAsync(raisedFor, ticket);
                }

                // Notify Department Head (or fallback to General)
                var head = ticket.Department?.DepartmentHead ?? generalDept?.DepartmentHead;
                if (head != null)
                {
                    await notificationService.SendEscalationAsync(head, ticket, "Automated SLA Breach");
                }

                // Notify Admin
                var adminEmail = _configuration["AdminSettings:Email"];
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    var adminUser = await unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.Email == adminEmail, stoppingToken);
                    if (adminUser != null)
                    {
                        await notificationService.SendSlaBreachAsync(adminUser, ticket);
                        await notificationService.SendEscalationAsync(adminUser, ticket, "Automated SLA Breach");
                    }
                    else
                    {
                        var fallbackAdmin = new User { Email = adminEmail, FirstName = "Admin" };
                        await notificationService.SendSlaBreachAsync(fallbackAdmin, ticket);
                        await notificationService.SendEscalationAsync(fallbackAdmin, ticket, "Automated SLA Breach");
                    }
                }
            }

            // 3. Process 30-min Unassigned Critical Tickets
            var criticalUnassignedThreshold = now.AddMinutes(-30);
            var criticalTickets = await unitOfWork.Tickets.Query()
                .Include(t => t.Department)
                    .ThenInclude(d => d.DepartmentHead)
                .Where(t => t.Status == TicketStatus.Open 
                            && t.Priority == TicketPriority.Critical
                            && !t.AssignedToAgentId.HasValue
                            && t.CreatedAt <= criticalUnassignedThreshold
                            && !t.IsEscalated)
                .ToListAsync(stoppingToken);

            foreach (var ticket in criticalTickets)
            {
                ticket.IsEscalated = true;
                ticket.EscalatedAt = now;
                ticket.EscalationReason = "System Auto-Escalation (Critical Unassigned > 30m)";
                ticket.LastEscalatedStatus = ticket.Status;

                await unitOfWork.Tickets.UpdateAsync(ticket);
                
                var logUserId = ticket.CreatedByUserId;

                await auditLogService.LogAsync(
                    AuditEventType.TicketEscalated,
                    AuditEntityType.Ticket,
                    ticket.Id,
                    "System Auto-Escalation (Critical Unassigned > 30m) triggered.",
                    logUserId,
                    ticket.Id);

                // Notify Department Head (or fallback to General)
                var headEmail = ticket.Department?.DepartmentHead?.Email ?? generalDept?.DepartmentHead?.Email;
                if (!string.IsNullOrEmpty(headEmail))
                {
                    var headUser = ticket.Department?.DepartmentHead ?? generalDept?.DepartmentHead;
                    if (headUser != null)
                        await notificationService.SendEscalationAsync(headUser, ticket, "System Auto-Escalation (Critical Unassigned > 30m)");
                }

                // Notify Admin
                var adminEmail = _configuration["AdminSettings:Email"];
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    var adminUser = await unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.Email == adminEmail, stoppingToken);
                    if (adminUser != null)
                    {
                        await notificationService.SendEscalationAsync(adminUser, ticket, "System Auto-Escalation (Critical Unassigned > 30m)");
                    }
                    else
                    {
                        var fallbackAdmin = new User { Email = adminEmail, FirstName = "Admin" };
                        await notificationService.SendEscalationAsync(fallbackAdmin, ticket, "System Auto-Escalation (Critical Unassigned > 30m)");
                    }
                }
            }

            // 4. Process 3 Business Days On Hold
            var onHoldCandidates = await unitOfWork.Tickets.Query()
                .Include(t => t.Department)
                    .ThenInclude(d => d.DepartmentHead)
                .Where(t => t.Status == TicketStatus.OnHold 
                            && !t.IsEscalated)
                .ToListAsync(stoppingToken);

            var settings = await unitOfWork.SystemSettings.Query().ToListAsync(stoppingToken);
            var start = TimeSpan.Parse(settings.FirstOrDefault(s => s.Key == "BusinessHoursStart")?.Value ?? "09:00");
            var end = TimeSpan.Parse(settings.FirstOrDefault(s => s.Key == "BusinessHoursEnd")?.Value ?? "17:00");
            var businessHoursPerDay = (end - start).TotalHours;
            var threeBusinessDaysHours = businessHoursPerDay * 3;

            var onHoldTickets = new List<Ticket>();
            foreach (var t in onHoldCandidates)
            {
                var elapsedHours = await slaEngine.CalculateBusinessHoursElapsedAsync(t.LastUpdatedAt, now);
                if (elapsedHours >= threeBusinessDaysHours)
                {
                    onHoldTickets.Add(t);
                }
            }

            foreach (var ticket in onHoldTickets)
            {
                ticket.IsEscalated = true;
                ticket.EscalatedAt = now;
                ticket.EscalationReason = "System Auto-Escalation (> 3 Days On Hold)";
                ticket.LastEscalatedStatus = ticket.Status;
                ticket.BumpPriority();

                await unitOfWork.Tickets.UpdateAsync(ticket);
                
                var logUserId = ticket.CreatedByUserId;

                await auditLogService.LogAsync(
                    AuditEventType.TicketEscalated,
                    AuditEntityType.Ticket,
                    ticket.Id,
                    "System Auto-Escalation (> 3 Days On Hold) triggered.",
                    logUserId,
                    ticket.Id);

                // Notify Department Head (or fallback to General)
                var headEmail = ticket.Department?.DepartmentHead?.Email ?? generalDept?.DepartmentHead?.Email;
                if (!string.IsNullOrEmpty(headEmail))
                {
                    var headUser = ticket.Department?.DepartmentHead ?? generalDept?.DepartmentHead;
                    if (headUser != null)
                        await notificationService.SendEscalationAsync(headUser, ticket, "System Auto-Escalation (> 3 Days On Hold)");
                }

                // Notify Admin
                var adminEmail = _configuration["AdminSettings:Email"];
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    var adminUser = await unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.Email == adminEmail, stoppingToken);
                    if (adminUser != null)
                    {
                        await notificationService.SendEscalationAsync(adminUser, ticket, "System Auto-Escalation (> 3 Days On Hold)");
                    }
                    else
                    {
                        var fallbackAdmin = new User { Email = adminEmail, FirstName = "Admin" };
                        await notificationService.SendEscalationAsync(fallbackAdmin, ticket, "System Auto-Escalation (> 3 Days On Hold)");
                    }
                }
            }

            await unitOfWork.SaveChangesAsync();
        }
    }
}

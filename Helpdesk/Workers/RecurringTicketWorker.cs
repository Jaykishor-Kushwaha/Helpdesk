using Cronos;
using Helpdesk.Enums;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Workers
{
    public class RecurringTicketWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RecurringTicketWorker> _logger;

        public RecurringTicketWorker(IServiceProvider serviceProvider, ILogger<RecurringTicketWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RecurringTicketWorker starting.");

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessRecurringTemplatesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing recurring templates.");
                }
            }
        }

        private async Task ProcessRecurringTemplatesAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Checking for scheduled recurring templates...");

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var slaEngine = scope.ServiceProvider.GetRequiredService<ISlaCalculationEngine>();
            var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var now = DateTime.UtcNow;

            var activeTemplates = await unitOfWork.RecurringTemplates.Query()
                .Where(t => t.IsActive 
                            && t.StartDate <= now 
                            && (t.EndDate == null || t.EndDate >= now)
                            && (t.MaxOccurrences == null || t.RunCount < t.MaxOccurrences))
                .ToListAsync(stoppingToken);

            var adminEmail = configuration["AdminSettings:Email"];
            var adminUser = await unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.Email == adminEmail, stoppingToken);
            var systemUserId = adminUser?.Id ?? 1;

            foreach (var template in activeTemplates)
            {
                try
                {
                    var expression = CronExpression.Parse(template.CronExpression);
                    var lastRun = template.LastRunAt ?? template.StartDate;
                    if (lastRun.Kind != DateTimeKind.Utc)
                    {
                        lastRun = DateTime.SpecifyKind(lastRun, DateTimeKind.Utc);
                    }

                    var nextOccurrence = expression.GetNextOccurrence(lastRun, inclusive: false);

                    if (nextOccurrence.HasValue && nextOccurrence.Value <= now)
                    {
                        var nextBusinessDay = await slaEngine.CalculateNextBusinessDayStartAsync(nextOccurrence.Value);
                        if (nextBusinessDay > now)
                        {
                            // Defer to next business morning
                            _logger.LogInformation($"Deferring template '{template.Name}' to next business day: {nextBusinessDay}");
                            continue;
                        }

                        _logger.LogInformation($"Generating ticket for template '{template.Name}'");

                        var ticket = new Ticket
                        {
                            Title = template.TicketTitle,
                            Description = template.Description,
                            Status = TicketStatus.Open,
                            Priority = template.Priority,
                            CategoryId = template.CategoryId,
                            AssignedToAgentId = template.AssignToAgentId,
                            RaisedForUserId = template.RaiseOnBehalfOfId,
                            CreatedByUserId = systemUserId,
                            CreatedAt = now,
                            LastUpdatedAt = now
                        };

                        // Resolve DepartmentId
                        int? deptId = null;
                        if (ticket.RaisedForUserId.HasValue)
                        {
                            var raisedFor = await unitOfWork.Users.GetByIdAsync(ticket.RaisedForUserId.Value);
                            if (raisedFor?.DepartmentId.HasValue == true) deptId = raisedFor.DepartmentId.Value;
                        }
                        if (!deptId.HasValue)
                        {
                            var creator = await unitOfWork.Users.GetByIdAsync(ticket.CreatedByUserId);
                            if (creator?.DepartmentId.HasValue == true) deptId = creator.DepartmentId.Value;
                        }
                        if (!deptId.HasValue)
                        {
                            var generalDept = await unitOfWork.Departments.Query().FirstOrDefaultAsync(d => d.Name == "General" && d.IsActive);
                            if (generalDept != null) deptId = generalDept.Id;
                        }

                        if (!deptId.HasValue || deptId <= 0)
                        {
                            _logger.LogWarning($"Skipping template '{template.Name}': Could not resolve an active Department for the generated ticket.");
                            continue;
                        }

                        ticket.DepartmentId = deptId.Value;
                        ticket.SLADeadline = await slaEngine.CalculateDeadlineAsync(now, ticket.Priority);

                        await unitOfWork.Tickets.AddAsync(ticket);
                        await unitOfWork.SaveChangesAsync(); // Save to get the ticket ID for the log
                        
                        var runLog = new RecurringTemplateRunLog
                        {
                            RecurringTemplateId = template.Id,
                            TemplateName = template.Name,
                            GeneratedTicketId = ticket.Id,
                            ScheduledFireTime = nextOccurrence.Value,
                            ActualFireTime = now
                        };
                        
                        await unitOfWork.RecurringTemplateRunLogs.AddAsync(runLog);

                        template.LastRunAt = now;
                        template.RunCount++;
                        if (template.MaxOccurrences.HasValue && template.RunCount >= template.MaxOccurrences.Value)
                            template.IsActive = false;
                        await unitOfWork.RecurringTemplates.UpdateAsync(template);

                        await auditLogService.LogAsync(
                            AuditEventType.RecurringTemplateRun,
                            AuditEntityType.Ticket,
                            ticket.Id,
                            $"Recurring template '{template.Name}' generated ticket #{ticket.Id}.",
                            systemUserId,
                            ticket.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process template '{template.Name}'");
                }
            }

            await unitOfWork.SaveChangesAsync();
            
            // Note: In a fully distributed system, we would grab the newly created ticket IDs and log Audit trails here if necessary.
            // For now, EF handles the save synchronously.
        }
    }
}

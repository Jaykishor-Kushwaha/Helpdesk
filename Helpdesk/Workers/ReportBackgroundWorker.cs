using Helpdesk.Interfaces;
using Helpdesk.Services;

namespace Helpdesk.Workers
{
    public class ReportBackgroundWorker : BackgroundService
    {
        private readonly IReportQueue _reportQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReportBackgroundWorker> _logger;
        private readonly IConfiguration _configuration;

        public ReportBackgroundWorker(IReportQueue reportQueue, IServiceProvider serviceProvider, ILogger<ReportBackgroundWorker> logger, IConfiguration configuration)
        {
            _reportQueue = reportQueue;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReportBackgroundWorker is starting.");

            // Create export directory if not exists
            var exportPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports");
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var request = await _reportQueue.DequeueAsync(stoppingToken);

                    if (request != null)
                    {
                        await ProcessReportAsync(request, exportPath);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken is canceled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred processing report queue.");
                }
            }
        }

        private async Task ProcessReportAsync(ReportRequest request, string exportPath)
        {
            using var scope = _serviceProvider.CreateScope();
            var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            try
            {
                _logger.LogInformation($"Processing async report: {request.ReportToken}");

                var (content, mimeType) = await reportingService.BuildReportCoreAsync(request.Filter);

                var extension = request.Filter.Format.ToLower() == "csv" ? "csv" : "pdf";
                var fileName = $"{request.ReportToken}.{extension}";
                var filePath = Path.Combine(exportPath, fileName);

                await File.WriteAllBytesAsync(filePath, content);

                // Fetch User email
                var user = await unitOfWork.Users.GetByIdAsync(request.RequestingUserId);
                if (user != null)
                {
                    // Get base URL if possible or hardcode for now
                    var downloadLink = $"http://localhost:5000/api/reports/download/{request.ReportToken}";

                    await notificationService.QueueEmailAsync(
                        user.Email,
                        "Your Async Report is Ready",
                        $"Your requested {request.Filter.Format.ToUpper()} report is complete. You can download it here: {downloadLink}\nThis link expires in 24 hours."
                    );
                }

                _logger.LogInformation($"Async report {request.ReportToken} finished and email queued.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process async report {request.ReportToken}");
            }
        }
    }
}

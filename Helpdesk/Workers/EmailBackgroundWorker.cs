using Helpdesk.Enums;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Text.RegularExpressions;

namespace Helpdesk.Workers
{
    public class EmailBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailBackgroundWorker> _logger;

        public EmailBackgroundWorker(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<EmailBackgroundWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Background Worker started.");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessOutboxAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing email outbox.");
                }
            }
        }

        private async Task ProcessOutboxAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var now = DateTime.UtcNow;
            var pendingQuery = unitOfWork.NotificationOutboxes.Query()
                .Where(n => n.Status == NotificationStatus.Pending ||
                           (n.Status == NotificationStatus.Failed && n.RetryCount < 3 && (n.NextRetryAt == null || n.NextRetryAt <= now)))
                .OrderBy(n => n.CreatedAt)
                .Take(50);

            var pendingEmails = await pendingQuery.ToListAsync(stoppingToken);

            // CA1860: Use Count > 0 instead of Any()
            if (pendingEmails.Count == 0) return;

            var dbSettings = await unitOfWork.SystemSettings.Query().ToListAsync(stoppingToken);
            string? GetSetting(string key) => dbSettings.FirstOrDefault(s => s.Key == key)?.Value;

            var emailSettings = _configuration.GetSection("EmailSettings");
            var host = GetSetting("SmtpServer") ?? emailSettings["SmtpServer"] ?? "smtp.gmail.com";
            var port = int.TryParse(GetSetting("SmtpPort") ?? emailSettings["SmtpPort"], out var p) ? p : 587;
            var username = GetSetting("SmtpUsername") ?? emailSettings["SmtpUsername"];
            var password = GetSetting("SmtpPassword") ?? emailSettings["SmtpPassword"];
            var senderEmail = GetSetting("SenderEmail") ?? GetSetting("SupportEmail") ?? emailSettings["SenderEmail"] ?? username;
            var fromName = GetSetting("SenderName") ?? GetSetting("SystemName") ?? emailSettings["SenderName"] ?? "Helpdesk System";
            var enableSsl = bool.TryParse(GetSetting("EnableSsl") ?? emailSettings["EnableSsl"] ?? "true", out var ssl) ? ssl : true;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("SMTP credentials not configured in appsettings.json. Skipping email dispatch.");
                return;
            }

            using var smtpClient = new SmtpClient();
            try
            {
                var options = enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await smtpClient.ConnectAsync(host, port, options, stoppingToken);
                await smtpClient.AuthenticateAsync(username, password.Replace(" ", ""), stoppingToken);

                foreach (var email in pendingEmails)
                {
                    try
                    {
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(fromName, senderEmail));
                        message.To.Add(new MailboxAddress("", email.RecipientEmail));
                        message.Subject = email.Subject;

                        var bodyBuilder = new BodyBuilder
                        {
                            HtmlBody = email.Body,
                            TextBody = ToPlainText(email.Body)
                        };
                        message.Body = bodyBuilder.ToMessageBody();

                        await smtpClient.SendAsync(message, stoppingToken);

                        email.Status = NotificationStatus.Sent;
                        email.ProcessedAt = DateTime.UtcNow;
                        email.ErrorMessage = null;
                        await unitOfWork.NotificationOutboxes.UpdateAsync(email);

                        _logger.LogInformation("Email sent to {Recipient} | Subject: {Subject}", email.RecipientEmail, email.Subject);
                    }
                    catch (Exception ex)
                    {
                        email.Status = NotificationStatus.Failed;
                        email.RetryCount++;
                        email.ErrorMessage = ex.Message;
                        email.ProcessedAt = DateTime.UtcNow;

                        if (email.RetryCount == 1) email.NextRetryAt = DateTime.UtcNow.AddSeconds(30);
                        else if (email.RetryCount == 2) email.NextRetryAt = DateTime.UtcNow.AddMinutes(2);
                        else email.NextRetryAt = DateTime.UtcNow.AddMinutes(10);

                        await unitOfWork.NotificationOutboxes.UpdateAsync(email);

                        _logger.LogError(ex, "Failed to send email to {Recipient} | Subject: {Subject}", email.RecipientEmail, email.Subject);
                    }
                }

                await smtpClient.DisconnectAsync(true, stoppingToken);

                // Fix: SaveChangesAsync moved inside try so sent statuses are always persisted
                await unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect, authenticate, or process SMTP dispatch at {Host}:{Port}.", host, port);

                foreach (var email in pendingEmails)
                {
                    if (email.Status != NotificationStatus.Sent)
                    {
                        email.Status = NotificationStatus.Failed;
                        email.RetryCount++;
                        email.ErrorMessage = $"SMTP Transport Failure: {ex.Message}";
                        email.ProcessedAt = DateTime.UtcNow;

                        if (email.RetryCount == 1) email.NextRetryAt = DateTime.UtcNow.AddSeconds(30);
                        else if (email.RetryCount == 2) email.NextRetryAt = DateTime.UtcNow.AddMinutes(2);
                        else email.NextRetryAt = DateTime.UtcNow.AddMinutes(10);

                        await unitOfWork.NotificationOutboxes.UpdateAsync(email);
                    }
                }

                await unitOfWork.SaveChangesAsync();
            }
        }

        private static string ToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var withBreaks = Regex.Replace(html, @"<\s*br\s*/?\s*>|</\s*p\s*>|</\s*div\s*>", Environment.NewLine, RegexOptions.IgnoreCase);
            var noTags = Regex.Replace(withBreaks, "<.*?>", string.Empty);
            return System.Net.WebUtility.HtmlDecode(noTags).Trim();
        }
    }
}

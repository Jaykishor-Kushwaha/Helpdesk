using Helpdesk.Enums;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using System.Text;

using Microsoft.Extensions.Configuration;

namespace Helpdesk.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IConfiguration _configuration;

        public NotificationService(IUnitOfWork unitOfWork, IEmailTemplateService emailTemplateService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _emailTemplateService = emailTemplateService;
            _configuration = configuration;
        }

        private string GetTicketUrl(int ticketId) => $"{_configuration["AppSettings:FrontendUrl"]}/tickets/{ticketId}";
        private static NotificationPreferences GetEffectivePreferences(User user) => user.NotificationPreferences ?? new NotificationPreferences();

        private async Task CreateInAppNotificationAsync(int userId, string title, string message, int? ticketId = null)
        {
            if (userId <= 0) return;

            var inAppNotification = new InAppNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                TicketId = ticketId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.InAppNotifications.AddAsync(inAppNotification);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task QueueEmailAsync(string recipientEmail, string subject, string htmlBody, string plainTextBody = null)
        {
            if (string.IsNullOrEmpty(recipientEmail)) return;

            // 100kB hard size guard (PRD constraint)
            if (Encoding.UTF8.GetByteCount(htmlBody) > 100 * 1024)
            {
                throw new InvalidOperationException("Email body exceeds the 100kB size limit.");
            }

            var outboxMessage = new NotificationOutbox
            {
                RecipientEmail = recipientEmail,
                Subject = subject,
                Body = htmlBody, // The worker will now send this. If plainText is needed by worker, worker should extract or we update schema.
                Status = NotificationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            await _unitOfWork.NotificationOutboxes.AddAsync(outboxMessage);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task SendTicketCreatedAsync(User user, Ticket ticket)
        {
            await CreateInAppNotificationAsync(user.Id, "Ticket Created", $"Ticket #{ticket.Id} - '{ticket.Title}' has been created.", ticket.Id);

            if (!GetEffectivePreferences(user).EmailOnTicketCreated) return;
            var t = _emailTemplateService.RenderTicketCreated(user.FirstName ?? user.Email, ticket.Id, ticket.Title, ticket.Status.ToString(), ticket.Priority.ToString(), ticket.Category?.Name ?? "N/A", ticket.SLADeadline?.ToString("g") ?? "N/A", GetTicketUrl(ticket.Id));
            await QueueEmailAsync(user.Email, $"Ticket Created: {ticket.Title}", t.HtmlBody, t.PlainText);
        }

        public async Task SendStatusChangedAsync(User user, Ticket ticket, string oldStatus)
        {
            await CreateInAppNotificationAsync(user.Id, "Ticket Status Updated", $"Ticket #{ticket.Id} - '{ticket.Title}' status changed from '{oldStatus}' to '{ticket.Status}'.", ticket.Id);

            if (!GetEffectivePreferences(user).EmailOnStatusChange) return;
            var t = _emailTemplateService.RenderTicketStatusChanged(user.FirstName ?? user.Email, ticket.Id, ticket.Title, oldStatus, ticket.Status.ToString(), GetTicketUrl(ticket.Id));
            await QueueEmailAsync(user.Email, $"Ticket Status Updated: {ticket.Title}", t.HtmlBody, t.PlainText);
        }

        public async Task SendNewCommentAsync(User user, Ticket ticket, Comment comment)
        {
            var authorName = comment.AuthorUser?.FirstName ?? "Someone";
            var snippet = comment.Content.Length > 200 ? comment.Content.Substring(0, 200) + "..." : comment.Content;
            await CreateInAppNotificationAsync(user.Id, "New Comment Posted", $"New comment on Ticket #{ticket.Id} by {authorName}: \"{snippet}\"", ticket.Id);

            if (!GetEffectivePreferences(user).EmailOnComment) return;
            var t = _emailTemplateService.RenderNewComment(user.FirstName ?? user.Email, ticket.Id, ticket.Title, authorName, comment.Content, GetTicketUrl(ticket.Id));
            await QueueEmailAsync(user.Email, $"New Comment on Ticket: {ticket.Title}", t.HtmlBody, t.PlainText);
        }

        public async Task SendAssignmentAsync(User user, Ticket ticket)
        {
            if (user.Id == ticket.AssignedToAgentId)
            {
                await CreateInAppNotificationAsync(user.Id, "Ticket Assigned", $"Ticket #{ticket.Id} - '{ticket.Title}' is assigned to you.", ticket.Id);

                if (!GetEffectivePreferences(user).EmailOnAssignment) return;
                var t = _emailTemplateService.RenderTicketAssigned(user.FirstName ?? user.Email, ticket.Id, ticket.Title, ticket.Priority.ToString(), ticket.SLADeadline?.ToString("g") ?? "N/A", GetTicketUrl(ticket.Id));
                await QueueEmailAsync(user.Email, $"Ticket Assigned To You: {ticket.Title}", t.HtmlBody, t.PlainText);
            }
            else
            {
                var agentName = ticket.AssignedToAgent?.FirstName ?? "Unassigned";
                await CreateInAppNotificationAsync(user.Id, "Ticket Assigned", $"Ticket #{ticket.Id} - '{ticket.Title}' has been assigned to {agentName}.", ticket.Id);

                if (!GetEffectivePreferences(user).EmailOnAssignment) return;
                var t = _emailTemplateService.RenderAgentReassigned(user.FirstName ?? user.Email, ticket.Id, ticket.Title, "Unassigned", agentName, GetTicketUrl(ticket.Id));
                await QueueEmailAsync(user.Email, $"Ticket Assigned to Agent: {ticket.Title}", t.HtmlBody, t.PlainText);
            }
        }

        public async Task SendSlaBreachAsync(User user, Ticket ticket)
        {
            await CreateInAppNotificationAsync(user.Id, "SLA Breach Alert", $"Ticket #{ticket.Id} - '{ticket.Title}' has breached its SLA deadline.", ticket.Id);

            // Mandatory event - always send
            var t = _emailTemplateService.RenderSlaBreach(user.FirstName ?? user.Email, ticket.Id, ticket.Title, ticket.Priority.ToString(), GetTicketUrl(ticket.Id));
            await QueueEmailAsync(user.Email, $"SLA Breach Alert: {ticket.Title}", t.HtmlBody, t.PlainText);
        }

        public async Task SendEscalationAsync(User user, Ticket ticket, string reason)
        {
            await CreateInAppNotificationAsync(user.Id, "Ticket Escalated", $"Ticket #{ticket.Id} - '{ticket.Title}' has been escalated. Reason: {reason}", ticket.Id);

            // Mandatory event - always send
            var t = _emailTemplateService.RenderTicketEscalated(user.FirstName ?? user.Email, ticket.Id, ticket.Title, reason, ticket.Priority.ToString(), GetTicketUrl(ticket.Id));
            await QueueEmailAsync(user.Email, $"Ticket Escalated: {ticket.Title}", t.HtmlBody, t.PlainText);
        }

        public async Task SendWelcomeEmailAsync(User user, string loginUrl, string? tempPassword = null)
        {
            // Mandatory event - always send
            var t = _emailTemplateService.RenderAccountWelcome(user.FirstName ?? user.Email, loginUrl, tempPassword);
            await QueueEmailAsync(user.Email, "Welcome to Helpdesk Support", t.HtmlBody, t.PlainText);
        }

        public async Task SendDeactivationEmailAsync(User user)
        {
            // Mandatory event - always send
            var t = _emailTemplateService.RenderAccountDeactivated(user.FirstName ?? user.Email);
            await QueueEmailAsync(user.Email, "Account Deactivated", t.HtmlBody, t.PlainText);
        }

        public async Task SendTicketClosedAsync(User user, Ticket ticket, string resolutionSummary)
        {
            await CreateInAppNotificationAsync(user.Id, "Ticket Closed", $"Ticket #{ticket.Id} - '{ticket.Title}' has been closed. Resolution: {resolutionSummary}", ticket.Id);

            if (!GetEffectivePreferences(user).EmailOnStatusChange) return;
            var t = _emailTemplateService.RenderTicketClosed(user.FirstName ?? user.Email, ticket.Id, ticket.Title, resolutionSummary, GetTicketUrl(ticket.Id));
            await QueueEmailAsync(user.Email, $"Ticket Closed: {ticket.Title}", t.HtmlBody, t.PlainText);
        }

        public async Task SendTicketReopenedAsync(User user, Ticket ticket, string reason)
        {
            await CreateInAppNotificationAsync(user.Id, "Ticket Reopened", $"Ticket #{ticket.Id} - '{ticket.Title}' has been reopened. Reason: {reason}", ticket.Id);

            if (!GetEffectivePreferences(user).EmailOnStatusChange) return;
            var t = _emailTemplateService.RenderTicketReopened(user.FirstName ?? user.Email, ticket.Id, ticket.Title, reason, GetTicketUrl(ticket.Id));
            await QueueEmailAsync(user.Email, $"Ticket Reopened: {ticket.Title}", t.HtmlBody, t.PlainText);
        }

        public async Task SendAgentReassignedAsync(User user, Ticket ticket, User oldAgent, User newAgent)
        {
            var oldAgentName = oldAgent?.FirstName ?? "Unassigned";
            var newAgentName = newAgent?.FirstName ?? "Unassigned";
            await CreateInAppNotificationAsync(user.Id, "Agent Reassigned", $"Ticket #{ticket.Id} - '{ticket.Title}' has been reassigned from '{oldAgentName}' to '{newAgentName}'.", ticket.Id);

            if (!GetEffectivePreferences(user).EmailOnAssignment) return;
            var t = _emailTemplateService.RenderAgentReassigned(user.FirstName ?? user.Email, ticket.Id, ticket.Title, oldAgentName, newAgentName, GetTicketUrl(ticket.Id));
            await QueueEmailAsync(user.Email, $"Agent Reassigned: {ticket.Title}", t.HtmlBody, t.PlainText);
        }

        public async Task SendSlaWarningAsync(User user, Ticket ticket)
        {
            await CreateInAppNotificationAsync(user.Id, "SLA Warning Alert", $"Ticket #{ticket.Id} - '{ticket.Title}' is approaching its SLA deadline.", ticket.Id);

            // Mandatory event
            var t = _emailTemplateService.RenderSlaWarning(user.FirstName ?? user.Email, ticket.Id, ticket.Title, ticket.Priority.ToString(), GetTicketUrl(ticket.Id));
            await QueueEmailAsync(user.Email, $"SLA Warning: {ticket.Title}", t.HtmlBody, t.PlainText);
        }

        public async Task SendSurveyRequestAsync(User user, Ticket ticket, string surveyUrl)
        {
            await CreateInAppNotificationAsync(user.Id, "Survey Request", $"Please rate our service for Ticket #{ticket.Id}.", ticket.Id);

            if (GetEffectivePreferences(user).OptOutSurveys) return;
            var t = _emailTemplateService.RenderSurveyRequest(user.FirstName ?? user.Email, ticket.Id, ticket.Title, surveyUrl);
            await QueueEmailAsync(user.Email, $"How did we do? Survey for Ticket #{ticket.Id}", t.HtmlBody, t.PlainText);
        }
    }
}

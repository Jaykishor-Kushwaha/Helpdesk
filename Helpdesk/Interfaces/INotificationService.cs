using Helpdesk.Models;
using System.Threading.Tasks;

namespace Helpdesk.Interfaces
{
    public interface INotificationService
    {
        Task QueueEmailAsync(string recipientEmail, string subject, string htmlBody, string plainTextBody = null);
        
        // Trigger Matrix
        Task SendTicketCreatedAsync(User user, Ticket ticket);
        Task SendStatusChangedAsync(User user, Ticket ticket, string oldStatus);
        Task SendNewCommentAsync(User user, Ticket ticket, Comment comment);
        Task SendAssignmentAsync(User user, Ticket ticket);
        Task SendSlaBreachAsync(User user, Ticket ticket);
        Task SendEscalationAsync(User user, Ticket ticket, string reason);
        Task SendWelcomeEmailAsync(User user, string loginUrl, string? tempPassword = null);
        Task SendDeactivationEmailAsync(User user);

        // New PRD requirements
        Task SendTicketClosedAsync(User user, Ticket ticket, string resolutionSummary);
        Task SendTicketReopenedAsync(User user, Ticket ticket, string reason);
        Task SendAgentReassignedAsync(User user, Ticket ticket, User oldAgent, User newAgent);
        Task SendSlaWarningAsync(User user, Ticket ticket);
        Task SendSurveyRequestAsync(User user, Ticket ticket, string surveyUrl);
    }
}

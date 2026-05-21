namespace Helpdesk.Interfaces
{
    public interface IEmailTemplateService
    {
        (string HtmlBody, string PlainText) RenderTicketCreated(string recipientName, int ticketId, string title, string status, string priority, string category, string slaDeadline, string ticketUrl);
        (string HtmlBody, string PlainText) RenderTicketStatusChanged(string recipientName, int ticketId, string title, string oldStatus, string newStatus, string ticketUrl);
        (string HtmlBody, string PlainText) RenderTicketAssigned(string agentName, int ticketId, string title, string priority, string slaDeadline, string ticketUrl);
        (string HtmlBody, string PlainText) RenderTicketEscalated(string recipientName, int ticketId, string title, string reason, string priority, string ticketUrl);
        (string HtmlBody, string PlainText) RenderAccountWelcome(string recipientName, string loginUrl, string? tempPassword = null);
        (string HtmlBody, string PlainText) RenderAccountDeactivated(string recipientName);
        (string HtmlBody, string PlainText) RenderNewComment(string recipientName, int ticketId, string title, string authorName, string commentSnippet, string ticketUrl);
        (string HtmlBody, string PlainText) RenderSlaBreach(string recipientName, int ticketId, string title, string priority, string ticketUrl);
        (string HtmlBody, string PlainText) RenderTicketClosed(string recipientName, int ticketId, string title, string resolutionSummary, string ticketUrl);
        (string HtmlBody, string PlainText) RenderTicketReopened(string recipientName, int ticketId, string title, string reason, string ticketUrl);
        (string HtmlBody, string PlainText) RenderAgentReassigned(string recipientName, int ticketId, string title, string oldAgentName, string newAgentName, string ticketUrl);
        (string HtmlBody, string PlainText) RenderSlaWarning(string recipientName, int ticketId, string title, string priority, string ticketUrl);
        (string HtmlBody, string PlainText) RenderSurveyRequest(string recipientName, int ticketId, string title, string surveyUrl);
        (string HtmlBody, string PlainText) RenderSystemAlert(string alertTitle, string message, string entityName, string actionType, string statusText, string statusColor, string ctaText, string ctaUrl);
    }
}

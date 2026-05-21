using Helpdesk.Helpers;
using Helpdesk.Interfaces;

namespace Helpdesk.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        public (string HtmlBody, string PlainText) RenderTicketCreated(string recipientName, int ticketId, string title, string status, string priority, string category, string slaDeadline, string ticketUrl)
        {
            var html = EmailTemplateHelper.TicketCreated(recipientName, ticketId, title, status, priority, category, slaDeadline, ticketUrl);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderTicketStatusChanged(string recipientName, int ticketId, string title, string oldStatus, string newStatus, string ticketUrl)
        {
            var html = EmailTemplateHelper.TicketStatusChanged(recipientName, ticketId, title, oldStatus, newStatus, ticketUrl);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderTicketAssigned(string agentName, int ticketId, string title, string priority, string slaDeadline, string ticketUrl)
        {
            var html = EmailTemplateHelper.TicketAssigned(agentName, ticketId, title, priority, slaDeadline, ticketUrl);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderTicketEscalated(string recipientName, int ticketId, string title, string reason, string priority, string ticketUrl)
        {
            var html = EmailTemplateHelper.TicketEscalated(recipientName, ticketId, title, reason, priority, ticketUrl);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderAccountWelcome(string recipientName, string loginUrl, string? tempPassword = null)
        {
            var html = EmailTemplateHelper.AccountWelcome(recipientName, loginUrl, tempPassword);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderAccountDeactivated(string recipientName)
        {
            var html = EmailTemplateHelper.AccountDeactivated(recipientName);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderNewComment(string recipientName, int ticketId, string title, string authorName, string commentSnippet, string ticketUrl)
        {
            var html = EmailTemplateHelper.NewComment(recipientName, ticketId, title, authorName, commentSnippet, ticketUrl);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderSlaBreach(string recipientName, int ticketId, string title, string priority, string ticketUrl)
        {
            var html = EmailTemplateHelper.SlaBreach(recipientName, ticketId, title, priority, ticketUrl);
            return (html, GeneratePlainText(html));
        }
        public (string HtmlBody, string PlainText) RenderTicketClosed(string recipientName, int ticketId, string title, string resolutionSummary, string ticketUrl)
        {
            var html = EmailTemplateHelper.TicketClosed(recipientName, ticketId, title, resolutionSummary, ticketUrl);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderTicketReopened(string recipientName, int ticketId, string title, string reason, string ticketUrl)
        {
            var html = EmailTemplateHelper.TicketReopened(recipientName, ticketId, title, ticketUrl);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderAgentReassigned(string recipientName, int ticketId, string title, string oldAgentName, string newAgentName, string ticketUrl)
        {
            var html = EmailTemplateHelper.AgentReassigned(recipientName, ticketId, title, oldAgentName, newAgentName, ticketUrl);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderSlaWarning(string recipientName, int ticketId, string title, string priority, string ticketUrl)
        {
            var html = EmailTemplateHelper.SlaWarning(recipientName, ticketId, title, priority, ticketUrl);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderSurveyRequest(string recipientName, int ticketId, string title, string surveyUrl)
        {
            var html = EmailTemplateHelper.SurveyRequest(recipientName, ticketId, title, surveyUrl);
            return (html, GeneratePlainText(html));
        }

        public (string HtmlBody, string PlainText) RenderSystemAlert(string alertTitle, string message, string entityName, string actionType, string statusText, string statusColor, string ctaText, string ctaUrl)
        {
            var html = EmailTemplateHelper.SystemAlert(alertTitle, message, entityName, actionType, statusText, statusColor, ctaText, ctaUrl);
            return (html, GeneratePlainText(html));
        }
        
        private string GeneratePlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var withBreaks = System.Text.RegularExpressions.Regex.Replace(html, @"<\s*br\s*/?\s*>|</\s*p\s*>|</\s*div\s*>", Environment.NewLine, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var noTags = System.Text.RegularExpressions.Regex.Replace(withBreaks, "<.*?>", string.Empty);
            return System.Net.WebUtility.HtmlDecode(noTags).Trim();
        }
    }
}

namespace Helpdesk.Helpers
{
    public static class EmailTemplateHelper
    {
        private const string BaseTemplate = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
              <title>{TITLE}</title>
            </head>
            <body style="margin:0;padding:0;background-color:#f4f6f9;font-family:'Segoe UI',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f6f9;padding:40px 0;">
                <tr>
                  <td align="center">
                    <table width="600" cellpadding="0" cellspacing="0" style="background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">

                      <!-- Header -->
                      <tr>
                        <td style="background:linear-gradient(135deg,#4f46e5 0%,#7c3aed 100%);padding:36px 40px;text-align:center;">
                          <h1 style="margin:0;color:#ffffff;font-size:22px;font-weight:700;letter-spacing:0.5px;">HelpDesk Support</h1>
                          <p style="margin:6px 0 0;color:rgba(255,255,255,0.8);font-size:12px;letter-spacing:2px;text-transform:uppercase;">{HEADER_SUBTITLE}</p>
                        </td>
                      </tr>

                      <!-- Body -->
                      <tr>
                        <td style="padding:36px 40px;">
                          <p style="margin:0 0 8px;color:#374151;font-size:16px;font-weight:600;">Hi {RECIPIENT_NAME},</p>
                          <p style="margin:0 0 28px;color:#6b7280;font-size:14px;line-height:1.7;">{MESSAGE}</p>

                          <!-- Ticket Details Card -->
                          <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f8fafc;border-radius:10px;border:1px solid #e5e7eb;margin-bottom:28px;">
                            <tr><td style="padding:20px 24px;">
                              {DETAILS_ROWS}
                            </td></tr>
                          </table>

                          <!-- CTA Button -->
                          {CTA_BUTTON}
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style="background-color:#f8fafc;border-top:1px solid #e5e7eb;padding:20px 40px;text-align:center;">
                          <p style="margin:0;color:#9ca3af;font-size:12px;">Sent via <strong style="color:#6b7280;">HelpDesk Support Help Desk</strong></p>
                          <p style="margin:6px 0 0;color:#9ca3af;font-size:12px;">
                            <a href="#" style="color:#4f46e5;text-decoration:none;">Notification Settings</a>
                            &nbsp;•&nbsp; Reply to this email to respond.
                          </p>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;

        private static string DetailRow(string label, string value, string? badgeColor = null)
        {
            var valueHtml = badgeColor != null
                ? $"<span style='background-color:{badgeColor}20;color:{badgeColor};font-size:11px;font-weight:600;padding:3px 10px;border-radius:20px;text-transform:uppercase;letter-spacing:0.5px;'>{value}</span>"
                : $"<span style='color:#111827;font-size:14px;font-weight:500;'>{value}</span>";

            return $"""
                <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:12px;">
                  <tr>
                    <td width="130" style="color:#6b7280;font-size:13px;vertical-align:middle;">{label}</td>
                    <td style="vertical-align:middle;">{valueHtml}</td>
                  </tr>
                </table>
                """;
        }

        private static string CtaButton(string label, string url = "#") =>
            $"""
            <table width="100%" cellpadding="0" cellspacing="0">
              <tr>
                <td align="center" style="padding-top:8px;">
                  <a href="{url}" style="display:inline-block;background:linear-gradient(135deg,#4f46e5,#7c3aed);color:#ffffff;text-decoration:none;padding:13px 36px;border-radius:8px;font-size:14px;font-weight:600;letter-spacing:0.3px;">{label}</a>
                </td>
              </tr>
            </table>
            """;

        // Priority badge colors
        private static string PriorityColor(string priority) => priority.ToLower() switch
        {
            "critical" => "#dc2626",
            "high" => "#ea580c",
            "medium" => "#d97706",
            "low" => "#16a34a",
            _ => "#6b7280"
        };

        private static string StatusColor(string status) => status.ToLower() switch
        {
            "open" => "#2563eb",
            "inprogress" => "#7c3aed",
            "onhold" => "#d97706",
            "resolved" => "#16a34a",
            "closed" => "#6b7280",
            "escalated" => "#dc2626",
            "reopened" => "#ea580c",
            _ => "#6b7280"
        };

        // ── Template builders ────────────────────────────────────────────

        public static string TicketCreated(
            string recipientName,
            int ticketId,
            string title,
            string status,
            string priority,
            string category,
            string slaDeadline,
            string ticketUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Status", status, StatusColor(status)) +
                DetailRow("Priority", priority, PriorityColor(priority)) +
                DetailRow("Category", category) +
                DetailRow("SLA Deadline", slaDeadline);

            return BaseTemplate
                .Replace("{TITLE}", "Ticket Created")
                .Replace("{HEADER_SUBTITLE}", "Ticket Notification")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", "Your ticket has been created successfully. Our support team will review it shortly.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }

        public static string TicketStatusChanged(
            string recipientName,
            int ticketId,
            string title,
            string oldStatus,
            string newStatus,
            string ticketUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Old Status", oldStatus, StatusColor(oldStatus)) +
                DetailRow("New Status", newStatus, StatusColor(newStatus));

            return BaseTemplate
                .Replace("{TITLE}", "Ticket Status Updated")
                .Replace("{HEADER_SUBTITLE}", "Status Update")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", $"Your ticket status has been updated from <strong>{oldStatus}</strong> to <strong>{newStatus}</strong>.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }

        public static string TicketAssigned(
            string agentName,
            int ticketId,
            string title,
            string priority,
            string slaDeadline,
            string ticketUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Priority", priority, PriorityColor(priority)) +
                DetailRow("SLA Deadline", slaDeadline);

            return BaseTemplate
                .Replace("{TITLE}", "Ticket Assigned To You")
                .Replace("{HEADER_SUBTITLE}", "Assignment Notification")
                .Replace("{RECIPIENT_NAME}", agentName)
                .Replace("{MESSAGE}", "A ticket has been assigned to you. Please review the details below and take action promptly.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }

        public static string TicketEscalated(
            string recipientName,
            int ticketId,
            string title,
            string reason,
            string priority,
            string ticketUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Priority", priority, PriorityColor(priority)) +
                DetailRow("Reason", reason);

            return BaseTemplate
                .Replace("{TITLE}", "Ticket Escalated")
                .Replace("{HEADER_SUBTITLE}", "Escalation Alert")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", "A ticket has been manually escalated and requires immediate attention.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }

        public static string TicketReopened(
            string recipientName,
            int ticketId,
            string title,
            string ticketUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Status", "Reopened", StatusColor("reopened"));

            return BaseTemplate
                .Replace("{TITLE}", "Ticket Reopened")
                .Replace("{HEADER_SUBTITLE}", "Ticket Update")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", "Your ticket has been reopened by an Administrator. Our support team will follow up shortly.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }

        public static string TicketResolved(
            string recipientName,
            int ticketId,
            string title,
            string ticketUrl,
            string? kbArticleTitle = null)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Status", "Resolved", StatusColor("resolved")) +
                (kbArticleTitle != null ? DetailRow("Resolved via KB", kbArticleTitle) : "");

            return BaseTemplate
                .Replace("{TITLE}", "Ticket Resolved")
                .Replace("{HEADER_SUBTITLE}", "Resolution Notice")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", "Your ticket has been resolved. If you have further questions, you can reopen it from the portal.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }

        public static string SlaOverridden(
            string recipientName,
            int ticketId,
            string title,
            string newDeadline,
            string reason,
            string ticketUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("New Deadline", newDeadline) +
                DetailRow("Reason", reason);

            return BaseTemplate
                .Replace("{TITLE}", "SLA Deadline Updated")
                .Replace("{HEADER_SUBTITLE}", "SLA Notice")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", "The SLA deadline for your ticket has been manually updated by an Administrator.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }

        public static string SurveyRequest(
            string recipientName,
            int ticketId,
            string title,
            string surveyUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Status", "Resolved", StatusColor("resolved"));

            return BaseTemplate
                .Replace("{TITLE}", "How did we do?")
                .Replace("{HEADER_SUBTITLE}", "Feedback Request")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", "Your ticket has been resolved. We'd love to hear about your experience — it takes less than a minute!")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("Take the Survey", surveyUrl));
        }
        public static string AccountWelcome(string recipientName, string loginUrl, string? tempPassword = null)
        {
            var details = DetailRow("Status", "Active", StatusColor("resolved"));
            if (!string.IsNullOrEmpty(tempPassword))
            {
                details += DetailRow("Temporary Password", tempPassword);
            }

            var message = "Your account has been successfully created. You can now log in to the HelpDesk portal to submit and track support requests.";
            if (!string.IsNullOrEmpty(tempPassword))
            {
                message += "<br/><br/>Please use the temporary password below to log in and change your password immediately.";
            }

            return BaseTemplate
                .Replace("{TITLE}", "Welcome to HelpDesk")
                .Replace("{HEADER_SUBTITLE}", "Account Activation")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", message)
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("Log In to HelpDesk", loginUrl));
        }

        public static string AccountDeactivated(string recipientName)
        {
            var details = DetailRow("Status", "Deactivated", StatusColor("escalated"));

            return BaseTemplate
                .Replace("{TITLE}", "Account Deactivated")
                .Replace("{HEADER_SUBTITLE}", "Account Status")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", "Your HelpDesk account has been deactivated by an Administrator. If you believe this is a mistake, please contact IT support.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", string.Empty);
        }

        public static string SystemAlert(string alertTitle, string message, string entityName, string actionType, string statusText, string statusColor, string ctaText, string ctaUrl)
        {
            var details = 
                DetailRow("Target Entity", entityName) +
                DetailRow("Action", actionType) +
                DetailRow("Final Status", statusText, StatusColor(statusColor));

            return BaseTemplate
                .Replace("{TITLE}", alertTitle)
                .Replace("{HEADER_SUBTITLE}", "System Notification")
                .Replace("{RECIPIENT_NAME}", "Admin")
                .Replace("{MESSAGE}", message)
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton(ctaText, ctaUrl));
        }

        public static string NewComment(string recipientName, int ticketId, string title, string authorName, string commentSnippet, string ticketUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Update By", authorName);

            var message = $"A new update has been posted to your ticket by <strong>{authorName}</strong>:<br/><br/><blockquote style='border-left:4px solid #4f46e5;padding-left:12px;color:#4b5563;font-style:italic;'>{commentSnippet}</blockquote>";

            return BaseTemplate
                .Replace("{TITLE}", "New Ticket Update")
                .Replace("{HEADER_SUBTITLE}", "Ticket Comment")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", message)
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }

        public static string SlaBreach(string recipientName, int ticketId, string title, string priority, string ticketUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Priority", priority, PriorityColor(priority)) +
                DetailRow("Status", "SLA Breached", StatusColor("escalated"));

            return BaseTemplate
                .Replace("{TITLE}", "SLA Breach Alert")
                .Replace("{HEADER_SUBTITLE}", "SLA Violation")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", "A ticket has exceeded its Service Level Agreement (SLA) deadline and requires immediate attention.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }

        public static string TicketClosed(string recipientName, int ticketId, string title, string resolutionSummary, string ticketUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Status", "Closed", StatusColor("closed")) +
                DetailRow("Resolution", resolutionSummary);

            return BaseTemplate
                .Replace("{TITLE}", "Ticket Closed")
                .Replace("{HEADER_SUBTITLE}", "Closure Notice")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", "Your ticket has been officially closed. Thank you for using HelpDesk Support.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }

        public static string AgentReassigned(string recipientName, int ticketId, string title, string oldAgentName, string newAgentName, string ticketUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Previous Assignee", oldAgentName) +
                DetailRow("New Assignee", newAgentName);

            return BaseTemplate
                .Replace("{TITLE}", "Agent Reassigned")
                .Replace("{HEADER_SUBTITLE}", "Assignment Update")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", $"Ticket #{ticketId} has been reassigned to a new agent.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }

        public static string SlaWarning(string recipientName, int ticketId, string title, string priority, string ticketUrl)
        {
            var details =
                DetailRow("Ticket ID", $"#{ticketId:X8}") +
                DetailRow("Subject", title) +
                DetailRow("Priority", priority, PriorityColor(priority)) +
                DetailRow("Warning Level", "75% of SLA Duration", StatusColor("reopened"));

            return BaseTemplate
                .Replace("{TITLE}", "SLA Warning Alert")
                .Replace("{HEADER_SUBTITLE}", "SLA Notice")
                .Replace("{RECIPIENT_NAME}", recipientName)
                .Replace("{MESSAGE}", $"Ticket #{ticketId} is nearing its SLA deadline. Please act promptly to avoid a breach.")
                .Replace("{DETAILS_ROWS}", details)
                .Replace("{CTA_BUTTON}", CtaButton("View Ticket Details", ticketUrl));
        }
    }
}
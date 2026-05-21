using Helpdesk.Interfaces;
using Helpdesk.Models;
using Helpdesk.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace Helpdesk.Services
{
    public partial class ReportingService : IReportingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IReportQueue _reportQueue;
        private readonly IAuditLogService _auditLogService;

        public ReportingService(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IReportQueue reportQueue, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _reportQueue = reportQueue;
            _auditLogService = auditLogService;
        }

        public async Task<ReportResult> GenerateExportAsync(Helpdesk.DTOs.ReportFilterDto filter)
        {
            var query = _unitOfWork.Tickets.Query();
            if (_currentUser.UserRole == Helpdesk.Enums.UserRole.Agent)
                query = query.Where(t => t.AssignedToAgentId == _currentUser.UserId);
            
            if (filter.StartDate.HasValue) query = query.Where(t => t.CreatedAt >= filter.StartDate.Value);
            if (filter.EndDate.HasValue) query = query.Where(t => t.CreatedAt <= filter.EndDate.Value);

            var count = await query.CountAsync();

            if (count > 10000)
            {
                var token = Guid.NewGuid().ToString("N");
                _reportQueue.Enqueue(new ReportRequest 
                {
                    ReportToken = token,
                    RequestingUserId = _currentUser.UserId,
                    Filter = filter
                });

                return new ReportResult
                {
                    IsAsyncProcessing = true,
                    Message = "Your report contains more than 10,000 rows and is being processed asynchronously. You will receive an email when it is ready.",
                    DownloadToken = token
                };
            }

            var (content, mimeType) = await BuildReportCoreAsync(filter);
            var extension = filter.Format.ToLower() == "csv" ? "csv" : "pdf";

            await _auditLogService.LogAsync(
                Helpdesk.Enums.AuditEventType.ReportExported,
                Helpdesk.Enums.AuditEntityType.SystemSetting, // Using SystemSetting as a generic entity for reports
                0,
                $"Generated {filter.Format.ToUpper()} report: {filter.ReportType}",
                _currentUser.UserId);

            return new ReportResult
            {
                IsAsyncProcessing = false,
                FileContent = content,
                MimeType = mimeType,
                FileName = $"Export_{filter.ReportType}_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}"
            };
        }

        public async Task<(byte[] content, string mimeType)> BuildReportCoreAsync(Helpdesk.DTOs.ReportFilterDto filter)
        {
            switch (filter.ReportType)
            {
                case Helpdesk.Enums.ReportType.KbUsage:
                    return await BuildKbUsageReportAsync(filter);
                case Helpdesk.Enums.ReportType.UserSatisfaction:
                    return await BuildUserSatisfactionReportAsync(filter);
                case Helpdesk.Enums.ReportType.Escalation:
                    return await BuildEscalationReportAsync(filter);
                case Helpdesk.Enums.ReportType.AgentPerformance:
                    return await BuildAgentPerformanceReportAsync(filter);
                case Helpdesk.Enums.ReportType.TicketVolume:
                    return await BuildTicketVolumeReportAsync(filter);
                case Helpdesk.Enums.ReportType.SlaCompliance:
                    return await BuildSlaComplianceReportAsync(filter);
                case Helpdesk.Enums.ReportType.Ageing:
                    return await BuildAgeingReportAsync(filter);
                case Helpdesk.Enums.ReportType.Category:
                    return await BuildCategoryReportAsync(filter);
                case Helpdesk.Enums.ReportType.DepartmentLoad:
                    return await BuildDepartmentLoadReportAsync(filter);
                default:
                    return await BuildStandardTicketReportAsync(filter);
            }
        }

        private async Task<(byte[] content, string mimeType)> BuildStandardTicketReportAsync(Helpdesk.DTOs.ReportFilterDto filter)
        {
            IQueryable<Ticket> query = _unitOfWork.Tickets.Query()
                .Include(t => t.Category)
                .Include(t => t.AssignedToAgent)
                .Include(t => t.RaisedForUser);

            if (_currentUser.UserRole == Helpdesk.Enums.UserRole.Agent)
                query = query.Where(t => t.AssignedToAgentId == _currentUser.UserId);

            if (filter.StartDate.HasValue) query = query.Where(t => t.CreatedAt >= filter.StartDate.Value);
            if (filter.EndDate.HasValue) query = query.Where(t => t.CreatedAt <= filter.EndDate.Value);
            if (filter.CategoryId.HasValue) query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
            if (filter.Priority.HasValue) query = query.Where(t => t.Priority == filter.Priority.Value);
            if (filter.AssignedToAgentId.HasValue) query = query.Where(t => t.AssignedToAgentId == filter.AssignedToAgentId.Value);
            if (filter.DepartmentId.HasValue) query = query.Where(t => t.DepartmentId == filter.DepartmentId.Value);

            var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            if (filter.Format.ToLower() == "csv")
            {
                var csv = GenerateCsv(tickets, filter.ReportType);
                return (Encoding.UTF8.GetBytes(csv), "text/csv");
            }
            else
            {
                var pdfBytes = GeneratePdf(tickets, filter.ReportType);
                return (pdfBytes, "application/pdf");
            }
        }

        private async Task<(byte[] content, string mimeType)> BuildKbUsageReportAsync(Helpdesk.DTOs.ReportFilterDto filter)
        {
            var articles = await _unitOfWork.KBArticles.Query().Include(k => k.Category).OrderByDescending(k => k.ViewCount).ToListAsync();
            var resolvedWithKb = await _unitOfWork.Tickets.Query()
                .Where(t => t.ResolvedViaKBArticleId.HasValue)
                .GroupBy(t => t.ResolvedViaKBArticleId)
                .Select(g => new { ArticleId = g.Key.Value, Count = g.Count() })
                .ToDictionaryAsync(x => x.ArticleId, x => x.Count);

            if (filter.Format.ToLower() == "csv")
            {
                var sb = new StringBuilder();
                sb.AppendLine("Report Type: KbUsage");
                sb.AppendLine("Id,Title,Category,Views,Helpful,NotHelpful,TicketsResolved");
                foreach (var a in articles)
                {
                    var resolvedCount = resolvedWithKb.GetValueOrDefault(a.Id, 0);
                    sb.AppendLine($"{a.Id},\"{a.Title?.Replace("\"", "\"\"")}\",\"{a.Category?.Name}\",{a.ViewCount},{a.HelpfulCount},{a.NotHelpfulCount},{resolvedCount}");
                }
                return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv");
            }
            else
            {
                var headers = new[] { "ID", "Title", "Category", "Views", "Helpful", "Not Helpful", "Resolved" };
                var rows = new List<string[]>();
                foreach (var a in articles)
                {
                    var resolvedCount = resolvedWithKb.GetValueOrDefault(a.Id, 0);
                    rows.Add(new[] { a.Id.ToString(), a.Title ?? "", a.Category?.Name ?? "", a.ViewCount.ToString(), a.HelpfulCount.ToString(), a.NotHelpfulCount.ToString(), resolvedCount.ToString() });
                }
                var pdfBytes = GenerateGenericTablePdf("Knowledge Base Usage Report", headers, rows);
                return (pdfBytes, "application/pdf");
            }
        }

        private async Task<(byte[] content, string mimeType)> BuildUserSatisfactionReportAsync(Helpdesk.DTOs.ReportFilterDto filter)
        {
            var query = _unitOfWork.Surveys.Query().Include(s => s.Ticket).ThenInclude(t => t.AssignedToAgent).AsQueryable();
            if (_currentUser.UserRole == Helpdesk.Enums.UserRole.Agent)
                query = query.Where(s => s.Ticket.AssignedToAgentId == _currentUser.UserId);
            if (filter.StartDate.HasValue) query = query.Where(s => s.SubmittedAt >= filter.StartDate.Value);
            if (filter.EndDate.HasValue) query = query.Where(s => s.SubmittedAt <= filter.EndDate.Value);
            if (filter.AssignedToAgentId.HasValue) query = query.Where(s => s.Ticket.AssignedToAgentId == filter.AssignedToAgentId.Value);

            var surveys = await query.OrderByDescending(s => s.SubmittedAt).ToListAsync();

            if (filter.Format.ToLower() == "csv")
            {
                var sb = new StringBuilder();
                sb.AppendLine("Report Type: UserSatisfaction");
                sb.AppendLine("TicketId,Agent,Score,Comments,SubmittedAt");
                foreach (var s in surveys)
                {
                    sb.AppendLine($"{s.TicketId},\"{s.Ticket?.AssignedToAgent?.FirstName} {s.Ticket?.AssignedToAgent?.LastName}\",{s.Score},\"{s.Comments?.Replace("\"", "\"\"")}\",{s.SubmittedAt:O}");
                }
                return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv");
            }
            else
            {
                var headers = new[] { "Ticket ID", "Agent", "Score", "Comments", "Submitted At" };
                var rows = new List<string[]>();
                foreach (var s in surveys)
                {
                    rows.Add(new[] { s.TicketId.ToString(), $"{s.Ticket?.AssignedToAgent?.FirstName} {s.Ticket?.AssignedToAgent?.LastName}", s.Score.ToString(), s.Comments ?? "", s.SubmittedAt.ToString("g") });
                }
                var pdfBytes = GenerateGenericTablePdf("User Satisfaction (CSAT) Report", headers, rows);
                return (pdfBytes, "application/pdf");
            }
        }

        private async Task<(byte[] content, string mimeType)> BuildAgentPerformanceReportAsync(Helpdesk.DTOs.ReportFilterDto filter)
        {
            var agentsQuery = _unitOfWork.Users.Query()
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Agent" || ur.Role.Name == "SupportAgent" || ur.Role.Name == "Admin"));

            if (filter.AssignedToAgentId.HasValue)
            {
                agentsQuery = agentsQuery.Where(u => u.Id == filter.AssignedToAgentId.Value);
            }
            if (_currentUser.UserRole == Helpdesk.Enums.UserRole.Agent)
            {
                agentsQuery = agentsQuery.Where(u => u.Id == _currentUser.UserId);
            }

            var agents = await agentsQuery.ToListAsync();

            var ticketsQuery = _unitOfWork.Tickets.Query();
            var surveysQuery = _unitOfWork.Surveys.Query();

            if (filter.StartDate.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.LastUpdatedAt >= filter.StartDate.Value);
                surveysQuery = surveysQuery.Where(s => s.SubmittedAt >= filter.StartDate.Value);
            }
            if (_currentUser.UserRole == Helpdesk.Enums.UserRole.Agent)
            {
                ticketsQuery = ticketsQuery.Where(t => t.AssignedToAgentId == _currentUser.UserId);
                surveysQuery = surveysQuery.Where(s => s.Ticket.AssignedToAgentId == _currentUser.UserId);
            }
            if (filter.EndDate.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.LastUpdatedAt <= filter.EndDate.Value);
                surveysQuery = surveysQuery.Where(s => s.SubmittedAt <= filter.EndDate.Value);
            }

            var ticketStats = await ticketsQuery
                .Where(t => t.AssignedToAgentId.HasValue && t.Status == Helpdesk.Enums.TicketStatus.Resolved)
                .GroupBy(t => t.AssignedToAgentId!.Value)
                .Select(g => new { AgentId = g.Key, ResolvedCount = g.Count() })
                .ToDictionaryAsync(x => x.AgentId, x => x.ResolvedCount);

            var surveyStats = await surveysQuery
                .Include(s => s.Ticket)
                .Where(s => s.Ticket.AssignedToAgentId.HasValue)
                .GroupBy(s => s.Ticket.AssignedToAgentId!.Value)
                .Select(g => new 
                { 
                    AgentId = g.Key, 
                    SurveyCount = g.Count(), 
                    AverageScore = g.Average(s => s.Score) 
                })
                .ToDictionaryAsync(x => x.AgentId, x => x);

            if (filter.Format.ToLower() == "csv")
            {
                var sb = new StringBuilder();
                sb.AppendLine("Report Type: AgentPerformance");
                sb.AppendLine("AgentId,AgentName,ResolvedTickets,SurveyCount,AverageCSAT");

                foreach (var agent in agents)
                {
                    var resolved = ticketStats.GetValueOrDefault(agent.Id, 0);
                    var sStat = surveyStats.GetValueOrDefault(agent.Id);
                    
                    string csatDisplay = "N/A";
                    int surveyCount = 0;

                    if (sStat != null)
                    {
                        surveyCount = sStat.SurveyCount;
                        if (sStat.SurveyCount >= 5)
                            csatDisplay = sStat.AverageScore.ToString("F2");
                    }

                    sb.AppendLine($"{agent.Id},\"{agent.FirstName} {agent.LastName}\",{resolved},{surveyCount},\"{csatDisplay}\"");
                }
                return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv");
            }
            else
            {
                var headers = new[] { "Agent ID", "Agent Name", "Resolved Tickets", "Survey Count", "Average CSAT" };
                var rows = new List<string[]>();
                foreach (var agent in agents)
                {
                    var resolved = ticketStats.GetValueOrDefault(agent.Id, 0);
                    var sStat = surveyStats.GetValueOrDefault(agent.Id);
                    string csatDisplay = "N/A";
                    int surveyCount = 0;
                    if (sStat != null)
                    {
                        surveyCount = sStat.SurveyCount;
                        if (sStat.SurveyCount >= 5)
                            csatDisplay = sStat.AverageScore.ToString("F2");
                    }
                    rows.Add(new[] { agent.Id.ToString(), $"{agent.FirstName} {agent.LastName}", resolved.ToString(), surveyCount.ToString(), csatDisplay });
                }
                var pdfBytes = GenerateGenericTablePdf("Agent Performance Report", headers, rows);
                return (pdfBytes, "application/pdf");
            }
        }

        private async Task<(byte[] content, string mimeType)> BuildEscalationReportAsync(Helpdesk.DTOs.ReportFilterDto filter)
        {
            var query = _unitOfWork.Tickets.Query()
                .Include(t => t.AssignedToAgent)
                .Include(t => t.Department)
                .Where(t => t.IsEscalated);

            if (_currentUser.UserRole == Helpdesk.Enums.UserRole.Agent)
                query = query.Where(t => t.AssignedToAgentId == _currentUser.UserId);

            if (filter.StartDate.HasValue) query = query.Where(t => t.CreatedAt >= filter.StartDate.Value);
            if (filter.EndDate.HasValue) query = query.Where(t => t.CreatedAt <= filter.EndDate.Value);
            if (filter.AssignedToAgentId.HasValue) query = query.Where(t => t.AssignedToAgentId == filter.AssignedToAgentId.Value);
            if (filter.DepartmentId.HasValue) query = query.Where(t => t.DepartmentId == filter.DepartmentId.Value);

            var escalated = await query.OrderByDescending(t => t.EscalatedAt).ToListAsync();

            if (filter.Format.ToLower() == "csv")
            {
                var sb = new StringBuilder();
                sb.AppendLine("Report Type: Escalation");
                sb.AppendLine("TicketId,Title,Department,AssignedTo,EscalatedAt,Reason");
                foreach (var t in escalated)
                {
                    sb.AppendLine($"{t.Id},\"{t.Title?.Replace("\"", "\"\"")}\",\"{t.Department?.Name}\",\"{t.AssignedToAgent?.FirstName} {t.AssignedToAgent?.LastName}\",{t.EscalatedAt:O},\"{t.EscalationReason?.Replace("\"", "\"\"")}\"");
                }
                return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv");
            }
            else
            {
                var headers = new[] { "Ticket ID", "Title", "Department", "Assigned To", "Escalated At", "Reason" };
                var rows = new List<string[]>();
                foreach (var t in escalated)
                {
                    rows.Add(new[] { t.Id.ToString(), t.Title ?? "", t.Department?.Name ?? "", $"{t.AssignedToAgent?.FirstName} {t.AssignedToAgent?.LastName}", t.EscalatedAt?.ToString("g") ?? "", t.EscalationReason ?? "" });
                }
                var pdfBytes = GenerateGenericTablePdf("Escalation Report", headers, rows);
                return (pdfBytes, "application/pdf");
            }
        }

        private string GenerateCsv(List<Ticket> tickets, Helpdesk.Enums.ReportType reportType)
        {
            var sb = new StringBuilder();
            // UTF-8 with BOM is standard for Excel CSV compatibility
            sb.AppendLine($"Report Type: {reportType}");
            sb.AppendLine("Id,Title,Status,Priority,Category,AssignedTo,RaisedFor,CreatedAt,IsEscalated");

            foreach (var t in tickets)
            {
                sb.AppendLine($"{t.Id},\"{t.Title?.Replace("\"", "\"\"")}\",{t.Status},{t.Priority},\"{t.Category?.Name}\",\"{t.AssignedToAgent?.FirstName} {t.AssignedToAgent?.LastName}\",\"{t.RaisedForUser?.FirstName} {t.RaisedForUser?.LastName}\",{t.CreatedAt:O},{t.IsEscalated}");
            }

            return sb.ToString();
        }

        private byte[] GeneratePdf(List<Ticket> tickets, Helpdesk.Enums.ReportType reportType)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(x => ComposeHeader(x, reportType));
                    page.Content().Element(x => ComposeContent(x, tickets));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private void ComposeHeader(IContainer container, Helpdesk.Enums.ReportType reportType)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Helpdesk v2.0").FontSize(20).SemiBold().FontColor(Colors.Indigo.Darken2);
                    column.Item().Text($"{reportType} Report").FontSize(14).FontColor(Colors.Grey.Darken2);
                    column.Item().Text($"Generated: {DateTime.UtcNow:g}");
                });
            });
        }

        private void ComposeContent(IContainer container, List<Ticket> tickets)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(20);

                var total = tickets.Count;
                var open = tickets.Count(t => t.Status == Helpdesk.Enums.TicketStatus.Open);
                var escalated = tickets.Count(t => t.IsEscalated);

                column.Item().Text($"Total Tickets: {total} | Open: {open} | Escalated: {escalated}").SemiBold();

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50);
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("ID");
                        header.Cell().Element(CellStyle).Text("Title");
                        header.Cell().Element(CellStyle).Text("Status");
                        header.Cell().Element(CellStyle).Text("Priority");

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    foreach (var t in tickets)
                    {
                        table.Cell().Element(CellStyle).Text(t.Id.ToString());
                        table.Cell().Element(CellStyle).Text(t.Title);
                        table.Cell().Element(CellStyle).Text(t.Status.ToString());
                        table.Cell().Element(CellStyle).Text(t.Priority.ToString());

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    }
                });
            });
        }

        private byte[] GenerateGenericTablePdf(string reportTitle, string[] headers, List<string[]> rows)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(x =>
                    {
                        x.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("Helpdesk v2.0").FontSize(16).SemiBold().FontColor(Colors.Indigo.Darken2);
                                column.Item().Text(reportTitle).FontSize(12).FontColor(Colors.Grey.Darken2);
                                column.Item().Text($"Generated: {DateTime.UtcNow:g}");
                            });
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < headers.Length; i++)
                                columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var h in headers)
                            {
                                header.Cell().BorderBottom(1).BorderColor(Colors.Black).Padding(5).Text(h).SemiBold();
                            }
                        });

                        foreach (var row in rows)
                        {
                            foreach (var cell in row)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(cell ?? "");
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();
        }
    }
}

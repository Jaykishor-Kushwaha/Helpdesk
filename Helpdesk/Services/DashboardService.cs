using Helpdesk.DTOs;
using Helpdesk.Enums;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Helpdesk.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        private readonly ICurrentUserService _currentUser;
        private readonly IConfiguration _configuration;

        public DashboardService(
            IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            ICurrentUserService currentUser,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _currentUser = currentUser;
            _configuration = configuration;
        }

        public async Task<DashboardDto> GetDashboardStatsAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);

            var role = _currentUser.UserRole;

            // FIX: guard plain User role — falls through to unfiltered query otherwise.
            // Throw here if users should not access the dashboard at all, or add a
            // filter (e.g. t.CreatedByUserId == userId) if they should see their own.
            if (role == UserRole.User)
                throw new UnauthorizedAccessException("Access to the dashboard is restricted.");

            var baseQuery = _unitOfWork.Tickets.Query();

            if (role == UserRole.Agent)
            {
                baseQuery = baseQuery.Where(t => t.AssignedToAgentId == _currentUser.UserId);
            }
            else if (role == UserRole.DepartmentHead)
            {
                var deptIds = await _unitOfWork.Departments.Query()
                    .Where(d => d.DepartmentHeadId == _currentUser.UserId)
                    .Select(d => d.Id)
                    .ToListAsync();

                baseQuery = baseQuery.Where(t => deptIds.Contains(t.DepartmentId));
            }

            // FIX: single DB round-trip for all ticket counts — replaces 11 separate
            // CountAsync calls that each scanned the same filtered ticket set.
            var tickets = await baseQuery
                .Select(t => new
                {
                    t.Status,
                    t.Priority,
                    t.CreatedAt,
                    t.AssignedToAgentId,
                    t.IsEscalated,
                    t.SLADeadline
                })
                .ToListAsync();

            var totalTickets = tickets.Count;
            var unassignedTickets = tickets.Count(t => t.AssignedToAgentId == null);
            var openTickets = tickets.Count(t => t.Status == TicketStatus.Open);
            var inProgressTickets = tickets.Count(t => t.Status == TicketStatus.InProgress);
            var onHoldTickets = tickets.Count(t => t.Status == TicketStatus.OnHold);
            var resolvedTickets = tickets.Count(t => t.Status == TicketStatus.Resolved);
            var closedTickets = tickets.Count(t => t.Status == TicketStatus.Closed);
            var lowPriority = tickets.Count(t => t.Priority == TicketPriority.Low);
            var mediumPriority = tickets.Count(t => t.Priority == TicketPriority.Medium);
            var highPriority = tickets.Count(t => t.Priority == TicketPriority.High);
            var criticalPriority = tickets.Count(t => t.Priority == TicketPriority.Critical);
            var ticketsThisMonth = tickets.Count(t => t.CreatedAt >= startOfMonth);
            var ticketsLastMonth = tickets.Count(t => t.CreatedAt >= startOfLastMonth && t.CreatedAt < startOfMonth);
            var escalatedTickets = tickets.Count(t => t.IsEscalated);
            var slaBreachedTickets = tickets.Count(t =>
                t.SLADeadline.HasValue &&
                t.SLADeadline < now &&
                t.Status != TicketStatus.Resolved &&
                t.Status != TicketStatus.Closed);

            // Category breakdown — still needs the navigation property so runs separately
            var categoryGroups = await baseQuery
                .Include(t => t.Category)
                .GroupBy(t => t.Category != null ? t.Category.Name : "Unknown")
                .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                .ToListAsync();

            var ticketsByCategory = categoryGroups.ToDictionary(g => g.CategoryName, g => g.Count);

            // Top agents by resolved count this month
            var topAgentData = await baseQuery
                .Where(t => t.Status == TicketStatus.Resolved
                         && t.AssignedToAgentId != null
                         && t.LastUpdatedAt >= startOfMonth)
                .GroupBy(t => t.AssignedToAgentId!.Value)
                .Select(g => new { AgentId = g.Key, ResolvedCount = g.Count() })
                .OrderByDescending(a => a.ResolvedCount)
                .Take(5)
                .ToListAsync();

            // FIX: keep IDs as int throughout — avoids ToString() conversions that
            // are unreliable across EF providers and harder to read.
            var topAgentIds = topAgentData.Select(a => a.AgentId).ToList();

            var agentUsers = await _userManager.Users
                .Where(u => topAgentIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync();

            var userLookup = agentUsers.ToDictionary(u => u.Id);

            var agentSurveys = await _unitOfWork.Surveys.Query()
                .Where(s => s.SubmittedAt >= startOfMonth)
                .Include(s => s.Ticket)
                .Where(s => s.Ticket.AssignedToAgentId != null
                         && topAgentIds.Contains(s.Ticket.AssignedToAgentId.Value))
                .GroupBy(s => s.Ticket.AssignedToAgentId!.Value)
                .Select(g => new
                {
                    AgentId = g.Key,
                    Count = g.Count(),
                    AverageScore = g.Average(s => s.Score)
                })
                .ToListAsync();

            var surveyLookup = agentSurveys.ToDictionary(s => s.AgentId);

            // FIX: read minimum survey threshold from config instead of hardcoding 5.
            // Key: DashboardSettings:MinSurveysForCsat — defaults to 5 if absent.
            var minSurveysForCsat = int.TryParse(
                _configuration["DashboardSettings:MinSurveysForCsat"], out var cfgMin) && cfgMin > 0
                ? cfgMin
                : 5;

            var topAgents = topAgentData.Select(a =>
            {
                userLookup.TryGetValue(a.AgentId, out var user);
                surveyLookup.TryGetValue(a.AgentId, out var surveyData);

                double? csat = null;
                if (surveyData != null && surveyData.Count >= minSurveysForCsat)
                    csat = surveyData.AverageScore;

                return new TopAgentDto
                {
                    AgentId = a.AgentId,
                    AgentName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                    ResolvedCount = a.ResolvedCount,
                    AverageCsat = csat
                };
            }).ToList();

            var allAgents = await _userManager.GetUsersInRoleAsync("Agent");
            var allUsers = await _userManager.GetUsersInRoleAsync("User");

            // FIX: AverageAsync runs the aggregation in the DB — no longer loads every
            // survey row into memory just to compute one average.
            var surveysThisMonth = _unitOfWork.Surveys.Query()
                .Where(s => s.SubmittedAt >= startOfMonth);

            var averageCsat = await surveysThisMonth.AnyAsync()
                ? await surveysThisMonth.AverageAsync(s => s.Score)
                : 0;

            return new DashboardDto
            {
                TotalTickets = totalTickets,
                OpenTickets = openTickets,
                InProgressTickets = inProgressTickets,
                OnHoldTickets = onHoldTickets,
                ResolvedTickets = resolvedTickets,
                ClosedTickets = closedTickets,
                LowPriorityTickets = lowPriority,
                MediumPriorityTickets = mediumPriority,
                HighPriorityTickets = highPriority,
                CriticalPriorityTickets = criticalPriority,
                TicketsThisMonth = ticketsThisMonth,
                TicketsLastMonth = ticketsLastMonth,
                TotalUsers = allUsers.Count,
                TotalAgents = allAgents.Count,
                UnassignedTickets = unassignedTickets,
                SlaBreachedTickets = slaBreachedTickets,
                EscalatedTickets = escalatedTickets,
                AverageCsat = Math.Round(averageCsat, 1),
                TicketsByCategory = ticketsByCategory,
                TopAgents = topAgents
            };
        }
    }
}
namespace Helpdesk.DTOs
{
    public class DashboardDto
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int OnHoldTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int LowPriorityTickets { get; set; }
        public int MediumPriorityTickets { get; set; }
        public int HighPriorityTickets { get; set; }
        public int CriticalPriorityTickets { get; set; }
        public int TicketsThisMonth { get; set; }
        public int TicketsLastMonth { get; set; }
        public int TotalUsers { get; set; }
        public int TotalAgents { get; set; }
        public int UnassignedTickets { get; set; }
        public int SlaBreachedTickets { get; set; }
        public int EscalatedTickets { get; set; }
        public double AverageCsat { get; set; }
        public Dictionary<string, int> TicketsByCategory { get; set; } = new();
        public List<TopAgentDto> TopAgents { get; set; } = new();
    }

    public class TopAgentDto
    {
        public int AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public int ResolvedCount { get; set; }
        public double? AverageCsat { get; set; }
    }
}
namespace Helpdesk.DTOs
{
    public class AuditLogDetailResponseDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }

    public class AuditLogResponseDto
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string PerformedByUserName { get; set; } = string.Empty;  
        public int? TicketId { get; set; }
        public string? IpAddress { get; set; }

        public IEnumerable<AuditLogDetailResponseDto> Details { get; set; } = new List<AuditLogDetailResponseDto>();
    }
}
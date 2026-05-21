using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class RecurringTemplateRunLog : IEntity
    {
        public int Id { get; set; }
        public int RecurringTemplateId { get; set; }
        
        [MaxLength(100)]
        public string TemplateName { get; set; } = string.Empty;
        
        public int GeneratedTicketId { get; set; }
        public DateTime ScheduledFireTime { get; set; }
        public DateTime ActualFireTime { get; set; }
    }
}

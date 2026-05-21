namespace Helpdesk.Models
{
    public class NotificationPreferences
    {
        public bool EmailOnTicketCreated { get; set; } = true;
        public bool EmailOnStatusChange { get; set; } = true;
        public bool EmailOnComment { get; set; } = true;
        public bool EmailOnAssignment { get; set; } = true;
        public bool OptOutSurveys { get; set; } = false;
    }
}

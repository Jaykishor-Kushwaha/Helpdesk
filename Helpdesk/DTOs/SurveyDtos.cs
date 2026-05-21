using System.ComponentModel.DataAnnotations;

namespace Helpdesk.DTOs
{
    public class SurveyResponseDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int? SubmittedByUserId { get; set; }
        public string? SubmittedByUserName { get; set; }
        public int Score { get; set; }
        public string? Comments { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class CreateSurveyResponseDto
    {
        public int TicketId { get; set; }
        [Range(1, 5)] public int Score { get; set; }
        [MaxLength(500)] public string? Comments { get; set; }
    }
}

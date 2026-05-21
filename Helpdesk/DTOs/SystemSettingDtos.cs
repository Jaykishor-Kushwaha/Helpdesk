using System.ComponentModel.DataAnnotations;

namespace Helpdesk.DTOs
{
    public class SystemSettingDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime LastUpdatedAt { get; set; }
    }

    public class UpdateSystemSettingDto
    {
        [Required] public string Value { get; set; } = string.Empty;
    }

    public class BulkUpdateSystemSettingsDto
    {
        public string? BusinessHoursStart { get; set; }
        public string? BusinessHoursEnd { get; set; }
        public string? WorkingDays { get; set; }
        public string? PublicHolidays { get; set; }
        
        public string? SystemName { get; set; }
        public string? LogoUrl { get; set; }
        public string? SupportEmail { get; set; }
        public string? Timezone { get; set; }
        public string? SessionTimeoutMinutes { get; set; }
        public string? SurveyDelayHours { get; set; }
        public string? SlaTargetCritical { get; set; }
        public string? SlaTargetHigh { get; set; }
        public string? SlaTargetMedium { get; set; }
        public string? SlaTargetLow { get; set; }
        public string? ArchivalPolicyMonths { get; set; }
    }

    public class SmtpSettingsDto
    {
        [Required] public string SmtpServer { get; set; } = "smtp.gmail.com";
        [Required] public int SmtpPort { get; set; } = 587;
        [Required] public string SmtpUsername { get; set; } = string.Empty;
        [Required] public string SmtpPassword { get; set; } = string.Empty;
        [Required] public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "Helpdesk System";
        public bool EnableSsl { get; set; } = true;
    }

    public class TestEmailDto
    {
        [Required]
        [EmailAddress]
        public string TestEmailAddress { get; set; } = string.Empty;
    }
}

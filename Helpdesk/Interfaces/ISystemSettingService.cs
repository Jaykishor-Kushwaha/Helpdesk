using Helpdesk.DTOs;

namespace Helpdesk.Interfaces
{
    public interface ISystemSettingService
    {
        Task<IEnumerable<SystemSettingDto>> GetAllAsync();
        Task<BulkUpdateSystemSettingsDto> GetBulkAsync();
        Task<BulkUpdateSystemSettingsDto> BulkUpdateAsync(BulkUpdateSystemSettingsDto dto);
        Task<SmtpSettingsDto> GetSmtpSettingsAsync();
        Task UpdateSmtpSettingsAsync(SmtpSettingsDto dto);
        Task<bool> SendTestEmailAsync(string testEmailAddress);
        Task UpdateLogoAsync(string logoUrl);
    }
}

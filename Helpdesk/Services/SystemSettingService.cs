using AutoMapper;
using Helpdesk.DTOs;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Services
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _auditLogService;
        private readonly ICurrentUserService _currentUser;
        private readonly IConfiguration _configuration;

        public SystemSettingService(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IAuditLogService auditLogService, 
            ICurrentUserService currentUser,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _auditLogService = auditLogService;
            _currentUser = currentUser;
            _configuration = configuration;
        }

        public async Task<IEnumerable<SystemSettingDto>> GetAllAsync()
        {
            var settings = await _unitOfWork.SystemSettings.Query().ToListAsync();
            return _mapper.Map<IEnumerable<SystemSettingDto>>(settings);
        }

        public async Task<BulkUpdateSystemSettingsDto> GetBulkAsync()
        {
            var settings = await _unitOfWork.SystemSettings.Query().ToListAsync();
            var dict = settings.ToDictionary(s => s.Key, s => s.Value);

            return new BulkUpdateSystemSettingsDto
            {
                BusinessHoursStart = dict.GetValueOrDefault("BusinessHoursStart"),
                BusinessHoursEnd = dict.GetValueOrDefault("BusinessHoursEnd"),
                WorkingDays = dict.GetValueOrDefault("WorkingDays"),
                PublicHolidays = dict.GetValueOrDefault("PublicHolidays"),
                SystemName = dict.GetValueOrDefault("SystemName"),
                LogoUrl = dict.GetValueOrDefault("LogoUrl"),
                SupportEmail = dict.GetValueOrDefault("SupportEmail"),
                Timezone = dict.GetValueOrDefault("Timezone"),
                SessionTimeoutMinutes = dict.GetValueOrDefault("SessionTimeoutMinutes"),
                SurveyDelayHours = dict.GetValueOrDefault("SurveyDelayHours"),
                SlaTargetCritical = dict.GetValueOrDefault("SlaTargetCritical"),
                SlaTargetHigh = dict.GetValueOrDefault("SlaTargetHigh"),
                SlaTargetMedium = dict.GetValueOrDefault("SlaTargetMedium"),
                SlaTargetLow = dict.GetValueOrDefault("SlaTargetLow"),
                ArchivalPolicyMonths = dict.GetValueOrDefault("ArchivalPolicyMonths")
            };
        }

        public async Task<BulkUpdateSystemSettingsDto> BulkUpdateAsync(BulkUpdateSystemSettingsDto dto)
        {
            var settings = await _unitOfWork.SystemSettings.Query().ToListAsync();
            var dict = settings.ToDictionary(s => s.Key);

            var auditDetails = new List<AuditLogDetail>();
            await UpdateOrAddSetting(dict, "BusinessHoursStart", dto.BusinessHoursStart, auditDetails);
            await UpdateOrAddSetting(dict, "BusinessHoursEnd", dto.BusinessHoursEnd, auditDetails);
            await UpdateOrAddSetting(dict, "WorkingDays", dto.WorkingDays, auditDetails);
            await UpdateOrAddSetting(dict, "PublicHolidays", dto.PublicHolidays, auditDetails);
            await UpdateOrAddSetting(dict, "SystemName", dto.SystemName, auditDetails);
            await UpdateOrAddSetting(dict, "LogoUrl", dto.LogoUrl, auditDetails);
            await UpdateOrAddSetting(dict, "SupportEmail", dto.SupportEmail, auditDetails);
            await UpdateOrAddSetting(dict, "Timezone", dto.Timezone, auditDetails);
            await UpdateOrAddSetting(dict, "SessionTimeoutMinutes", dto.SessionTimeoutMinutes, auditDetails);
            await UpdateOrAddSetting(dict, "SurveyDelayHours", dto.SurveyDelayHours, auditDetails);
            await UpdateOrAddSetting(dict, "SlaTargetCritical", dto.SlaTargetCritical, auditDetails);
            await UpdateOrAddSetting(dict, "SlaTargetHigh", dto.SlaTargetHigh, auditDetails);
            await UpdateOrAddSetting(dict, "SlaTargetMedium", dto.SlaTargetMedium, auditDetails);
            await UpdateOrAddSetting(dict, "SlaTargetLow", dto.SlaTargetLow, auditDetails);
            await UpdateOrAddSetting(dict, "ArchivalPolicyMonths", dto.ArchivalPolicyMonths, auditDetails);

            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogSystemSettingsChangedAsync("Bulk updated system settings.", _currentUser.UserId, auditDetails);

            return await GetBulkAsync();
        }

        private async Task UpdateOrAddSetting(Dictionary<string, SystemSetting> dict, string key, string? value, List<AuditLogDetail>? auditDetails = null)
        {
            if (value == null) return;

            if (dict.TryGetValue(key, out var setting))
            {
                var oldValue = setting.Value;
                setting.Value = value;
                setting.LastUpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SystemSettings.UpdateAsync(setting);
                if (auditDetails != null && oldValue != value)
                    auditDetails.Add(new AuditLogDetail { FieldName = key, OldValue = oldValue, NewValue = value });
            }
            else
            {
                var newSetting = new SystemSetting
                {
                    Key = key,
                    Value = value,
                    LastUpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.SystemSettings.AddAsync(newSetting);
                auditDetails?.Add(new AuditLogDetail { FieldName = key, OldValue = null, NewValue = value });
            }
        }

        private async Task SetSettingAsync(string key, string? value)
        {
            var setting = await _unitOfWork.SystemSettings.Query().FirstOrDefaultAsync(s => s.Key == key);
            if (setting != null)
            {
                setting.Value = value ?? string.Empty;
                setting.LastUpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SystemSettings.UpdateAsync(setting);
            }
            else
            {
                await _unitOfWork.SystemSettings.AddAsync(new SystemSetting { Key = key, Value = value ?? string.Empty, LastUpdatedAt = DateTime.UtcNow });
            }
        }
        public async Task UpdateLogoAsync(string logoUrl)
        {
            var oldValue = (await _unitOfWork.SystemSettings.Query().FirstOrDefaultAsync(s => s.Key == "LogoUrl"))?.Value;
            await SetSettingAsync("LogoUrl", logoUrl);
            await _unitOfWork.SaveChangesAsync();
            await _auditLogService.LogSystemSettingsChangedAsync(
                "Updated logo URL.",
                _currentUser.UserId,
                new List<AuditLogDetail> { new AuditLogDetail { FieldName = "LogoUrl", OldValue = oldValue, NewValue = logoUrl } });
        }
        public async Task<SmtpSettingsDto> GetSmtpSettingsAsync()
        {
            var settings = await _unitOfWork.SystemSettings.Query().ToListAsync();
            var dict = settings.ToDictionary(s => s.Key, s => s.Value);

            var dto = new SmtpSettingsDto();
            dto.SmtpServer = dict.GetValueOrDefault("SmtpServer") ?? _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            
            var portStr = dict.GetValueOrDefault("SmtpPort") ?? _configuration["EmailSettings:SmtpPort"];
            if (int.TryParse(portStr, out var port)) dto.SmtpPort = port;
            
            dto.SmtpUsername = dict.GetValueOrDefault("SmtpUsername") ?? _configuration["EmailSettings:SmtpUsername"] ?? string.Empty;
            dto.SmtpPassword = dict.GetValueOrDefault("SmtpPassword") ?? _configuration["EmailSettings:SmtpPassword"] ?? string.Empty;
            dto.SmtpPassword = dto.SmtpPassword.Replace(" ", ""); // Strip spaces from App Passwords

            dto.SenderEmail = dict.GetValueOrDefault("SenderEmail") ?? _configuration["EmailSettings:SenderEmail"] ?? string.Empty;
            dto.SenderName = dict.GetValueOrDefault("SenderName") ?? _configuration["EmailSettings:SenderName"] ?? "Helpdesk System";
            
            var sslStr = dict.GetValueOrDefault("EnableSsl") ?? _configuration["EmailSettings:EnableSsl"];
            if (bool.TryParse(sslStr, out var ssl)) dto.EnableSsl = ssl;

            return dto;
        }

        public async Task UpdateSmtpSettingsAsync(SmtpSettingsDto dto)
        {
            var current = await GetSmtpSettingsAsync();
            await SetSettingAsync("SmtpServer", dto.SmtpServer);
            await SetSettingAsync("SmtpPort", dto.SmtpPort.ToString());
            await SetSettingAsync("SmtpUsername", dto.SmtpUsername);
            await SetSettingAsync("SmtpPassword", dto.SmtpPassword);
            await SetSettingAsync("SenderEmail", dto.SenderEmail);
            await SetSettingAsync("SenderName", dto.SenderName);
            await SetSettingAsync("EnableSsl", dto.EnableSsl.ToString());
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogSystemSettingsChangedAsync(
                "Updated SMTP settings.",
                _currentUser.UserId,
                new List<AuditLogDetail>
                {
                    new AuditLogDetail { FieldName = "SmtpServer", OldValue = current.SmtpServer, NewValue = dto.SmtpServer },
                    new AuditLogDetail { FieldName = "SmtpPort", OldValue = current.SmtpPort.ToString(), NewValue = dto.SmtpPort.ToString() },
                    new AuditLogDetail { FieldName = "SmtpUsername", OldValue = current.SmtpUsername, NewValue = dto.SmtpUsername },
                    new AuditLogDetail { FieldName = "SenderEmail", OldValue = current.SenderEmail, NewValue = dto.SenderEmail },
                    new AuditLogDetail { FieldName = "SenderName", OldValue = current.SenderName, NewValue = dto.SenderName },
                    new AuditLogDetail { FieldName = "EnableSsl", OldValue = current.EnableSsl.ToString(), NewValue = dto.EnableSsl.ToString() }
                });
        }

        public async Task<bool> SendTestEmailAsync(string testEmailAddress)
        {
            var settings = await GetSmtpSettingsAsync();
            if (string.IsNullOrEmpty(settings.SmtpUsername) || string.IsNullOrEmpty(settings.SmtpPassword))
                throw new InvalidOperationException("SMTP credentials are not configured.");

            using var smtpClient = new MailKit.Net.Smtp.SmtpClient();
            var options = settings.EnableSsl ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto;
            
            await smtpClient.ConnectAsync(settings.SmtpServer, settings.SmtpPort, options);
            await smtpClient.AuthenticateAsync(settings.SmtpUsername, settings.SmtpPassword.Replace(" ", ""));

            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(settings.SenderName, settings.SenderEmail));
            message.To.Add(new MimeKit.MailboxAddress("", testEmailAddress));
            message.Subject = "Helpdesk SMTP Test";

            var bodyBuilder = new MimeKit.BodyBuilder { HtmlBody = "<h3>SMTP Test Successful!</h3><p>Your Helpdesk system is correctly configured to send emails.</p>" };
            message.Body = bodyBuilder.ToMessageBody();

            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);

            return true;
        }
    }
}

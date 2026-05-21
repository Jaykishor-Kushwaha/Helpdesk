using Helpdesk.DTOs;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SystemSettingsController : ControllerBase
    {
        private readonly ISystemSettingService _settingService;

        public SystemSettingsController(ISystemSettingService settingService)
        {
            _settingService = settingService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<SystemSettingDto>>>> GetAll()
        {
            var result = await _settingService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<SystemSettingDto>>.SuccessResponse(result));
        }

        [HttpGet("bulk")]
        public async Task<ActionResult<ApiResponse<BulkUpdateSystemSettingsDto>>> GetBulk()
        {
            var result = await _settingService.GetBulkAsync();
            return Ok(ApiResponse<BulkUpdateSystemSettingsDto>.SuccessResponse(result));
        }

        [HttpPut]
        public async Task<ActionResult<ApiResponse<BulkUpdateSystemSettingsDto>>> BulkUpdate([FromBody] BulkUpdateSystemSettingsDto dto)
        {
            var result = await _settingService.BulkUpdateAsync(dto);
            return Ok(ApiResponse<BulkUpdateSystemSettingsDto>.SuccessResponse(result, "Settings updated"));
        }

        [HttpGet("smtp")]
        public async Task<ActionResult<ApiResponse<SmtpSettingsDto>>> GetSmtp()
        {
            var result = await _settingService.GetSmtpSettingsAsync();
            return Ok(ApiResponse<SmtpSettingsDto>.SuccessResponse(result));
        }

        [HttpPut("smtp")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateSmtp([FromBody] SmtpSettingsDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            await _settingService.UpdateSmtpSettingsAsync(dto);
            return Ok(ApiResponse<object>.SuccessResponse(null!, "SMTP Settings updated successfully."));
        }

        [HttpPost("smtp/test")]
        public async Task<ActionResult<ApiResponse<object>>> SendTestEmail([FromBody] TestEmailDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _settingService.SendTestEmailAsync(dto.TestEmailAddress);
                return Ok(ApiResponse<object>.SuccessResponse(null!, "Test email sent successfully. Please check the inbox."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.FailResponse($"Failed to send test email: {ex.Message}"));
            }
        }

        [HttpPost("logo")]
        public async Task<ActionResult<ApiResponse<object>>> UploadLogo(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(ApiResponse<object>.FailResponse("No file uploaded"));
            
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"logo_{DateTime.UtcNow.Ticks}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            var logoUrl = $"/uploads/{fileName}";
            
            await _settingService.UpdateLogoAsync(logoUrl);
            
            return Ok(ApiResponse<object>.SuccessResponse(new { LogoUrl = logoUrl }, "Logo uploaded successfully"));
        }
    }
}

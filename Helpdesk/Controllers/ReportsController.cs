using Helpdesk.DTOs;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Agent")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;
        private readonly IWebHostEnvironment _env;

        public ReportsController(IReportingService reportingService, IWebHostEnvironment env)
        {
            _reportingService = reportingService;
            _env = env;
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] ReportFilterDto filter)
        {
            var allowedFormats = new[] { "pdf", "csv" };
            if (!allowedFormats.Contains(filter.Format.ToLower()))
            {
                return BadRequest(ApiResponse<object>.FailResponse("Invalid format. Allowed formats: pdf, csv"));
            }

            var result = await _reportingService.GenerateExportAsync(filter);

            if (result.IsAsyncProcessing)
            {
                return Accepted(ApiResponse<object>.SuccessResponse(
                    new { downloadToken = result.DownloadToken },
                    result.Message!));
            }

            return File(result.FileContent!, result.MimeType!, result.FileName!);
        }

        [HttpGet("download/{token}")]
        [AllowAnonymous] 
        public IActionResult DownloadAsyncReport(string token)
        {
            if (!Guid.TryParse(token, out _))
            {
                return BadRequest("Invalid download token.");
            }

            var exportPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "exports");
            
            var pdfPath = Path.Combine(exportPath, $"{token}.pdf");
            var csvPath = Path.Combine(exportPath, $"{token}.csv");

            string targetPath = null!;
            string mimeType = null!;
            string fileName = null!;

            if (System.IO.File.Exists(pdfPath))
            {
                targetPath = pdfPath;
                mimeType = "application/pdf";
                fileName = $"Export_{token}.pdf";
            }
            else if (System.IO.File.Exists(csvPath))
            {
                targetPath = csvPath;
                mimeType = "text/csv";
                fileName = $"Export_{token}.csv";
            }
            else
            {
                return NotFound("Report not found or has expired.");
            }

            var fileInfo = new FileInfo(targetPath);
            if (fileInfo.CreationTimeUtc < DateTime.UtcNow.AddHours(-24))
            {
                System.IO.File.Delete(targetPath);
                return BadRequest("Report download link has expired.");
            }

            return PhysicalFile(targetPath, mimeType, fileName);
        }
    }
}

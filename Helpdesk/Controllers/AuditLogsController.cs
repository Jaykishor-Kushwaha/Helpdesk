using Helpdesk.DTOs;
using Helpdesk.Enums;
using Helpdesk.Helper;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogsController : BaseController
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(
            IAuditLogService auditLogService,
            ICurrentUserService currentUser) : base(currentUser)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [Authorize(Roles = Roles.AdminOnly)]
        public async Task<IActionResult> GetAllAuditLogs(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? actorUserId,
            [FromQuery] AuditEventType? eventType,
            [FromQuery] AuditEntityType? entityType)
        {
            var logs = await _auditLogService.SearchAuditLogsAsync(startDate, endDate, actorUserId, eventType, entityType);

            return Ok(ApiResponse<IEnumerable<AuditLogResponseDto>>
                .SuccessResponse(logs));
        }
            [HttpGet("by-ticket")]
            [Authorize(Roles = Roles.AdminOnly)]
            public async Task<IActionResult> GetByTicket([FromQuery] int ticketId)
            {
                var logs = await _auditLogService.GetAuditLogsByTicketIdAsync(
                    new GetAuditLogsByTicketDto { TicketId = ticketId });
                return Ok(ApiResponse<IEnumerable<AuditLogResponseDto>>
                    .SuccessResponse(logs));
            }


            [HttpGet("by-user")]
            [Authorize(Roles = Roles.AdminOnly)]
            public async Task<IActionResult> GetByUser([FromQuery] int userId)
            {
                var logs = await _auditLogService.GetAuditLogsByUserIdAsync(
                    new GetAuditLogsByUserDto { UserId = userId });
                return Ok(ApiResponse<IEnumerable<AuditLogResponseDto>>
                    .SuccessResponse(logs));
            }

        [HttpGet("event/{eventType}")]
        [Authorize(Roles = Roles.AdminOnly)]
        public async Task<IActionResult> GetAuditLogsByEventType(AuditEventType eventType)
        {
            var logs = await _auditLogService.GetAuditLogsByEventTypeAsync(eventType);

            return Ok(ApiResponse<IEnumerable<AuditLogResponseDto>>
                .SuccessResponse(logs));
        }

        [HttpGet("export")]
        [Authorize(Roles = Roles.AdminOnly)]
        public async Task<IActionResult> ExportAuditLogs(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? actorUserId,
            [FromQuery] AuditEventType? eventType,
            [FromQuery] AuditEntityType? entityType)
        {
            var csvBytes = await _auditLogService.ExportAuditLogsAsync(startDate, endDate, actorUserId, eventType, entityType);
            var fileName = $"AuditLogs_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(csvBytes, "text/csv", fileName);
        }
    }
}

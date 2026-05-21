using Helpdesk.DTOs;
using Helpdesk.Exceptions;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public partial class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly ILogger<TicketsController> _logger;
        private readonly ICurrentUserService _currentUser;

        public TicketsController(ITicketService ticketService, ILogger<TicketsController> logger, ICurrentUserService currentUser)
        {
            _ticketService = ticketService;
            _logger = logger;
            _currentUser = currentUser;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Agent,User,DepartmentHead")]
        public async Task<ActionResult<ApiResponse<PagedResponse<TicketResponseDto>>>> GetAll(
            [FromQuery] TicketFilterDto filter)
        {
            try
            {
                var result = await _ticketService.FilterTicketsAsync(filter);
                return Ok(ApiResponse<PagedResponse<TicketResponseDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tickets");
                return StatusCode(500, ApiResponse<object>.FailResponse("An error occurred while retrieving tickets"));
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Agent,User,DepartmentHead")]
        public async Task<ActionResult<ApiResponse<TicketResponseDto>>> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse<object>.FailResponse("Valid ticket id is required"));

            try
            {
                var result = await _ticketService.GetTicketByIdAsync(
                    new GetTicketByIdDto
                    {
                        Id = id,
                        UserId = _currentUser.UserId,  
                        Role = _currentUser.UserRole  
                    });

                if (result == null)
                    return NotFound(ApiResponse<object>.FailResponse("Ticket not found"));

                return Ok(ApiResponse<TicketResponseDto>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ticket {TicketId}", id);
                return StatusCode(500, ApiResponse<object>.FailResponse("An error occurred while retrieving the ticket"));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Agent,User")]
        public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] CreateTicketDto dto)
        {
            if (dto == null)
                return BadRequest(ApiResponse<object>.FailResponse("Request body cannot be empty"));

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ? Normalize incoming values
            dto.NormalizeValues();

            try
            {
                var id = await _ticketService.CreateTicketAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id },
                    ApiResponse<int>.SuccessResponse(id, "Ticket created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation in Create: {Message}", ex.Message);
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket");
                return StatusCode(500,
                    ApiResponse<object>.FailResponse("An error occurred while creating the ticket"));
            }
        }

        [HttpPut]
        [Authorize(Roles = "Admin,Agent,User")]
        public async Task<ActionResult<ApiResponse<TicketResponseDto>>> Update([FromBody] UpdateTicketDto dto)
        {
            if (dto == null)
                return BadRequest(ApiResponse<object>.FailResponse("Request body cannot be empty"));

            if (dto.Id <= 0)
                return BadRequest(ApiResponse<object>.FailResponse("Valid ticket id is required"));

            // ? Normalize incoming values to remove invalid entries
            dto.NormalizeValues();

            try
            {
                var result = await _ticketService.UpdateTicketAsync(dto);
                return Ok(ApiResponse<TicketResponseDto>.SuccessResponse(result, "Ticket updated successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Ticket not found: {Message}", ex.Message);
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (ForbiddenException ex)
            {
                _logger.LogWarning("Access denied: {Message}", ex.Message);
                return StatusCode(403, ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation in Update: {Message}", ex.Message);
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket");
                return StatusCode(500, ApiResponse<object>.FailResponse("An error occurred while updating the ticket"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Agent,User")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse<object>.FailResponse("Valid ticket id is required"));

            try
            {
                var success = await _ticketService.DeleteTicketAsync(new GetByIdDto { Id = id });

                if (!success)
                    return NotFound(ApiResponse<object>.FailResponse("Ticket not found or insufficient permissions"));

                return Ok(ApiResponse<object>.SuccessResponse(null, "Ticket archived successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ticket {TicketId}", id);
                return StatusCode(500, ApiResponse<object>.FailResponse("An error occurred while deleting the ticket"));
            }
        }

        [HttpPost("escalate")]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<ActionResult<ApiResponse<bool>>> EscalateTicket([FromBody] EscalateTicketDto dto)
        {
            try
            {
                var result = await _ticketService.EscalateTicketAsync(dto);
                return Ok(ApiResponse<bool>.SuccessResponse(result, "Ticket successfully escalated."));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<bool>.FailResponse(ex.Message));
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, ApiResponse<bool>.FailResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<bool>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error escalating ticket {TicketId}", dto.TicketId);
                return StatusCode(500, ApiResponse<bool>.FailResponse("An error occurred while escalating the ticket."));
            }
        }

        [HttpPost("{id}/acknowledge-escalation")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> AcknowledgeEscalation(int id)
        {
            try
            {
                var result = await _ticketService.AcknowledgeEscalationAsync(id);
                if (!result)
                    return BadRequest(ApiResponse<bool>.FailResponse("Escalation could not be acknowledged."));

                return Ok(ApiResponse<bool>.SuccessResponse(result, "Escalation acknowledged."));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<bool>.FailResponse(ex.Message));
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, ApiResponse<bool>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging escalation for ticket {TicketId}", id);
                return StatusCode(500, ApiResponse<bool>.FailResponse("An error occurred while acknowledging the escalation."));
            }
        }

        [HttpPost("{id}/reopen")]
        [Authorize(Roles = "Admin,Agent,User")]
        public async Task<ActionResult<ApiResponse<bool>>> ReopenTicket(int id)
        {
            try
            {
                var result = await _ticketService.ReopenTicketAsync(id);
                if (!result)
                    return BadRequest(ApiResponse<bool>.FailResponse("Ticket could not be reopened."));

                return Ok(ApiResponse<bool>.SuccessResponse(result, "Ticket successfully reopened."));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<bool>.FailResponse(ex.Message));
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, ApiResponse<bool>.FailResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<bool>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reopening ticket {TicketId}", id);
                return StatusCode(500, ApiResponse<bool>.FailResponse("An error occurred while reopening the ticket."));
            }
        }

        [HttpPost("{id}/override-sla")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> OverrideSla(int id, [FromBody] OverrideSlaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _ticketService.OverrideSlaAsync(id, dto);
                return Ok(ApiResponse<bool>.SuccessResponse(result, "SLA deadline successfully overridden."));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<bool>.FailResponse(ex.Message));
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, ApiResponse<bool>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error overriding SLA for ticket {TicketId}", id);
                return StatusCode(500, ApiResponse<bool>.FailResponse("An error occurred while overriding the SLA deadline."));
            }
        }

        [HttpPost("{id}/resolve-via-kb/{kbId}")]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<ActionResult<ApiResponse<bool>>> ResolveViaKB(int id, int kbId)
        {
            try
            {
                var result = await _ticketService.ResolveViaKBAsync(id, kbId);
                return Ok(ApiResponse<bool>.SuccessResponse(result, "Ticket successfully resolved via KB article."));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<bool>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving ticket {TicketId} via KB", id);
                return StatusCode(500, ApiResponse<bool>.FailResponse("An error occurred while resolving the ticket via KB."));
            }
        }
    }
}

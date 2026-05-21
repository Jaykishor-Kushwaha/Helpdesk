using Helpdesk.DTOs;
using Helpdesk.Exceptions;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RecurringTemplatesController : ControllerBase
    {
        private readonly IRecurringTemplateService _templateService;

        public RecurringTemplatesController(IRecurringTemplateService templateService)
        {
            _templateService = templateService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<RecurringTemplateDto>>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _templateService.GetAllAsync(page, pageSize);
            return Ok(ApiResponse<PagedResponse<RecurringTemplateDto>>.SuccessResponse(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<RecurringTemplateDto>>> GetById(int id)
        {
            var result = await _templateService.GetByIdAsync(id);
            if (result == null) return NotFound(ApiResponse<object>.FailResponse("RecurringTemplate not found"));

            return Ok(ApiResponse<RecurringTemplateDto>.SuccessResponse(result));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<RecurringTemplateDto>>> Create([FromBody] CreateRecurringTemplateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _templateService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<RecurringTemplateDto>.SuccessResponse(result, "Template created"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPut]
        public async Task<ActionResult<ApiResponse<RecurringTemplateDto>>> Update([FromBody] UpdateRecurringTemplateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _templateService.UpdateAsync(dto.Id, dto);
                if (result == null) return NotFound(ApiResponse<object>.FailResponse("RecurringTemplate not found"));

                return Ok(ApiResponse<RecurringTemplateDto>.SuccessResponse(result, "Template updated"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            var success = await _templateService.DeleteAsync(id);
            if (!success) return NotFound(ApiResponse<object>.FailResponse("RecurringTemplate not found"));

            return Ok(ApiResponse<object>.SuccessResponse(null, "Template deleted"));
        }

        [HttpPost("{id}/trigger")]
        public async Task<ActionResult<ApiResponse<object>>> TriggerNow(int id)
        {
            try
            {
                await _templateService.TriggerNowAsync(id);
                return Ok(ApiResponse<object>.SuccessResponse(null, "Template triggered successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }
    }
}

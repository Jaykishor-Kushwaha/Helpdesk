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
    public class SurveysController : ControllerBase
    {
        private readonly ISurveyService _surveyService;

        public SurveysController(ISurveyService surveyService)
        {
            _surveyService = surveyService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<SurveyResponseDto>>>> GetAll([FromQuery] int? ticketId)
        {
            var result = await _surveyService.GetAllAsync(ticketId);
            return Ok(ApiResponse<IEnumerable<SurveyResponseDto>>.SuccessResponse(result));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SurveyResponseDto>>> Create([FromBody] CreateSurveyResponseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _surveyService.CreateAsync(dto);
                return Ok(ApiResponse<SurveyResponseDto>.SuccessResponse(result, "Survey submitted successfully"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }
    }
}

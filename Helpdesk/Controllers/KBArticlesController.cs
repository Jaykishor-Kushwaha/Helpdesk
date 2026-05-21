using Helpdesk.DTOs;
using Helpdesk.Exceptions;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public partial class KBArticlesController : ControllerBase
    {
        private readonly IKBArticleService _kbService;

        public KBArticlesController(IKBArticleService kbService)
        {
            _kbService = kbService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<KBArticleDto>>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] int? categoryId = null)
        {
            var result = await _kbService.GetAllAsync(page, pageSize, categoryId);
            return Ok(ApiResponse<PagedResponse<KBArticleDto>>.SuccessResponse(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<KBArticleDto>>> GetById(int id)
        {
            var result = await _kbService.GetByIdAsync(id);
            if (result == null) return NotFound(ApiResponse<object>.FailResponse("KBArticle not found"));

            return Ok(ApiResponse<KBArticleDto>.SuccessResponse(result));
        }

        [HttpPost("{id}/view")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> IncrementViewCount(int id)
        {
            await _kbService.IncrementViewCountAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "View count incremented"));
        }

        [HttpPost("{id}/feedback")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> SubmitFeedback(int id, [FromBody] KBFeedbackDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _kbService.SubmitFeedbackAsync(id, dto.IsHelpful);
                return Ok(ApiResponse<object>.SuccessResponse(null, "Feedback submitted successfully."));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<IEnumerable<KBArticleDto>>>> Search([FromQuery] string query)
        {
            var results = await _kbService.SearchAsync(query);
            return Ok(ApiResponse<IEnumerable<KBArticleDto>>.SuccessResponse(results));
        }

        [HttpGet("suggest")]
        public async Task<ActionResult<ApiResponse<IEnumerable<KBArticleDto>>>> SuggestKBArticles([FromQuery] string query)
        {
            var results = await _kbService.SuggestAsync(query);
            return Ok(ApiResponse<IEnumerable<KBArticleDto>>.SuccessResponse(results));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<ActionResult<ApiResponse<KBArticleDto>>> Create([FromBody] CreateKBArticleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _kbService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<KBArticleDto>.SuccessResponse(result, "KBArticle created"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPut]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<ActionResult<ApiResponse<KBArticleDto>>> Update([FromBody] UpdateKBArticleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _kbService.UpdateAsync(dto.Id, dto);
                if (result == null) return NotFound(ApiResponse<object>.FailResponse("KBArticle not found"));

                return Ok(ApiResponse<KBArticleDto>.SuccessResponse(result, "KBArticle updated"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            var success = await _kbService.DeleteAsync(id);
            if (!success) return NotFound(ApiResponse<object>.FailResponse("KBArticle not found"));

            return Ok(ApiResponse<object>.SuccessResponse(null, "KBArticle deleted"));
        }

        [HttpGet("{id}/versions")]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<ActionResult<ApiResponse<IEnumerable<KBArticleVersionDto>>>> GetVersions(int id)
        {
            var versions = await _kbService.GetArticleVersionsAsync(id);
            return Ok(ApiResponse<IEnumerable<KBArticleVersionDto>>.SuccessResponse(versions));
        }

        [HttpPost("{id}/revert/{versionNumber}")]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<ActionResult<ApiResponse<KBArticleDto>>> RevertVersion(int id, int versionNumber)
        {
            try
            {
                var result = await _kbService.RevertToVersionAsync(id, versionNumber);
                if (result == null) return NotFound(ApiResponse<object>.FailResponse("KBArticle or Version not found"));

                return Ok(ApiResponse<KBArticleDto>.SuccessResponse(result, $"Reverted to version {versionNumber}"));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [HttpPost("{id}/submit-review")]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<ActionResult<ApiResponse<KBArticleDto>>> SubmitForReview(int id)
        {
            var result = await _kbService.SubmitForReviewAsync(id);
            return Ok(ApiResponse<KBArticleDto>.SuccessResponse(result!, "KBArticle submitted for review"));
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<KBArticleDto>>> Approve(int id)
        {
            var result = await _kbService.ApproveAsync(id);
            return Ok(ApiResponse<KBArticleDto>.SuccessResponse(result!, "KBArticle approved"));
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<KBArticleDto>>> Reject(int id, [FromBody] KBRejectDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _kbService.RejectAsync(id, dto.Reason);
            return Ok(ApiResponse<KBArticleDto>.SuccessResponse(result!, "KBArticle rejected"));
        }
    }
}

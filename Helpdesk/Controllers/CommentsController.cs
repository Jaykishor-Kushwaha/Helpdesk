using Helpdesk.DTOs;
using Helpdesk.Helper;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [Authorize]
    [Route("api/comments")]
    [ApiController]
    public class CommentsController : BaseController
    {
        private readonly ICommentService _commentService;

        public CommentsController(
            ICommentService commentService,
            ICurrentUserService currentUser) : base(currentUser)
        {
            _commentService = commentService;
        }

        [HttpGet]
        [Authorize(Roles = Roles.All)]
        public async Task<IActionResult> GetComments(int ticketId)
        {
            var dto = new GetCommentsByTicketDto
            {
                TicketId = ticketId
            };

            var comments = await _commentService.GetCommentsByTicketIdAsync(dto);

            return Ok(ApiResponse<IEnumerable<CommentResponseDto>>
                .SuccessResponse(comments));
        }

        [HttpPost]
        [Authorize(Roles = Roles.All)]
        public async Task<IActionResult> AddComment([FromBody] AddCommentDto dto)
        {
            var comment = await _commentService.AddCommentAsync(dto);

            return Ok(ApiResponse<CommentResponseDto>
                .SuccessResponse(comment, "Comment added successfully"));
        }

        [HttpDelete]
        public async Task<ActionResult<ApiResponse<object>>> Delete(
            [FromQuery] int id)
        {
            var success = await _commentService.DeleteCommentAsync(
                new CommentActionDto { Id = id });

            if (!success)
                return NotFound(ApiResponse<object>.FailResponse("Not found"));

            return Ok(ApiResponse<object>.SuccessResponse(null, "Deleted"));
        }
    }
}
using Helpdesk.DTOs;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [ApiController]
    [Route("api/kb/suggest")]
    public class KBSuggestController : ControllerBase
    {
        private readonly IKBArticleService _kbService;

        public KBSuggestController(IKBArticleService kbService)
        {
            _kbService = kbService;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<IEnumerable<KBArticleDto>>>> Suggest([FromBody] string query)
        {
            var results = await _kbService.SuggestAsync(query);
            return Ok(ApiResponse<IEnumerable<KBArticleDto>>.SuccessResponse(results));
        }

        [HttpPost("record-solved")]
        public async Task<ActionResult<ApiResponse<object>>> RecordSolved([FromBody] RecordSolvedDto dto)
        {
            var success = await _kbService.RecordSolvedAsync(dto.TicketId, dto.ArticleId, dto.Solved);
            return Ok(ApiResponse<object>.SuccessResponse(new { dismissTicketForm = success }));
        }
    }

    public class RecordSolvedDto
    {
        public int TicketId { get; set; }
        public int ArticleId { get; set; }
        public bool Solved { get; set; }
    }
}

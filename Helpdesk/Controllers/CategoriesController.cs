using Helpdesk.DTOs;
using Helpdesk.Helper;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : BaseController
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(
            ICategoryService categoryService,
            ICurrentUserService currentUser) : base(currentUser)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();

            return Ok(ApiResponse<IEnumerable<CategoryResponseDto>>
                .SuccessResponse(categories));
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse<object>.FailResponse("Invalid ID"));
            var category = await _categoryService.GetCategoryByIdAsync(
                new GetByIdDto { Id = id });
            return Ok(ApiResponse<CategoryResponseDto>
                .SuccessResponse(category));
        }

        [HttpPost]
        [Authorize(Roles = Roles.AdminOnly)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            var category = await _categoryService.CreateCategoryAsync(dto);

            return Ok(ApiResponse<CategoryResponseDto>
                .SuccessResponse(category, "Created"));
        }

        [HttpPut]
        [Authorize(Roles = Roles.AdminOnly)]
        public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryDto dto)
        {
            await _categoryService.UpdateCategoryAsync(dto);

            return Ok(ApiResponse<object>
                .SuccessResponse(null, "Updated"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.AdminOnly)]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse<object>.FailResponse("Invalid ID"));
            await _categoryService.DeleteCategoryAsync(
                new GetByIdDto { Id = id });
            return Ok(ApiResponse<object>
                .SuccessResponse(null, "Deleted"));
        }
    }
}
using Helpdesk.DTOs;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentsController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentDto>>>> GetAll()
        {
            var result = await _departmentService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<DepartmentDto>>.SuccessResponse(result));
        }

        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentSummaryDto>>>> GetSummary()
        {
            var result = await _departmentService.GetDepartmentSummaryAsync();
            return Ok(ApiResponse<IEnumerable<DepartmentSummaryDto>>.SuccessResponse(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetById(int id)
        {
            var result = await _departmentService.GetByIdAsync(id);
            if (result == null) return NotFound(ApiResponse<object>.FailResponse("Department not found"));
            return Ok(ApiResponse<DepartmentDto>.SuccessResponse(result));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create([FromBody] CreateDepartmentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _departmentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<DepartmentDto>.SuccessResponse(result, "Department created"));
        }

        [HttpPut]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update([FromBody] UpdateDepartmentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _departmentService.UpdateAsync(dto.Id, dto);
            if (result == null) return NotFound(ApiResponse<object>.FailResponse("Department not found"));
            return Ok(ApiResponse<DepartmentDto>.SuccessResponse(result, "Department updated"));
        }

        [HttpPut("{id}/deactivate")]
        public async Task<ActionResult<ApiResponse<object>>> Deactivate(int id)
        {
            var success = await _departmentService.DeactivateAsync(id);
            if (!success) return NotFound(ApiResponse<object>.FailResponse("Department not found"));
            return Ok(ApiResponse<object>.SuccessResponse(null, "Department deactivated"));
        }
    }
}

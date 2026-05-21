using Helpdesk.DTOs;
using Helpdesk.Exceptions;
using Helpdesk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(ApiResponse<IEnumerable<UserResponseDto>>.SuccessResponse(users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                return StatusCode(500, ApiResponse<object>.FailResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get a specific user by ID (Any authenticated user)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        [Authorize] // ✅ Any authenticated user can view
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(ApiResponse<object>.FailResponse("Invalid Id"));

                var user = await _userService.GetUserByIdAsync(id);

                // ✅ User found - return 200 OK
                return Ok(ApiResponse<UserResponseDto>.SuccessResponse(user));
            }
            catch (NotFoundException ex)
            {
                // ✅ User not found - return 404 NOT FOUND
                _logger.LogWarning($"User with ID {id} not found");
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user with ID {id}");
                return StatusCode(500, ApiResponse<object>.FailResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Create a new user (Admin only)
        /// </summary>
        /// <param name="dto">User creation details</param>
        /// <returns>Created user with 201 status</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")] // ✅ Admin only
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(ApiResponse<object>.FailResponse("Invalid request body"));

                // ✅ ModelState validation
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
                    return BadRequest(ApiResponse<object>.FailResponse($"Validation failed: {errors}"));
                }

                var user = await _userService.CreateUserAsync(dto);

                // ✅ Return 201 CREATED with Location header
                return CreatedAtAction(
                    nameof(GetUserById),
                    new { id = user.Id },
                    ApiResponse<UserResponseDto>.SuccessResponse(user, "User created successfully"));
            }
            catch (ValidationException ex)
            {
                // ✅ Handle validation errors (e.g., duplicate email)
                _logger.LogWarning($"Validation error during user creation: {ex.Message}");
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, ApiResponse<object>.FailResponse("Internal server error"));
            }
        }

       
        [HttpPut] 
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(ApiResponse<object>.FailResponse("Invalid request body"));

                // Ensure the ID is provided in the DTO
                if (dto.Id <= 0)
                    return BadRequest(ApiResponse<object>.FailResponse("A valid User ID must be provided in the request body."));

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
                    return BadRequest(ApiResponse<object>.FailResponse($"Validation failed: {errors}"));
                }

                await _userService.UpdateUserAsync(dto);

                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user with ID {dto?.Id}");
                return StatusCode(500, ApiResponse<object>.FailResponse("Internal server error"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(ApiResponse<object>.FailResponse("Invalid Id"));

                await _userService.DeleteUserAsync(id);

                // ✅ Return 204 NO CONTENT
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning($"User with ID {id} not found during deletion");
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning($"Validation error during user deletion: {ex.Message}");
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user with ID {id}");
                return StatusCode(500, ApiResponse<object>.FailResponse("Internal server error"));
            }
        }

        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Admin")] // ✅ Admin only
        public async Task<IActionResult> ActivateUser(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(ApiResponse<object>.FailResponse("Invalid Id"));

                await _userService.UpdateUserAsync(new UpdateUserDto
                {
                    Id = id,
                    IsActive = true
                });

                return Ok(ApiResponse<object>.SuccessResponse(null!, "User activated successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning($"User with ID {id} not found during activation");
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning($"Validation error during user activation: {ex.Message}");
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error activating user with ID {id}");
                return StatusCode(500, ApiResponse<object>.FailResponse("Internal server error"));
            }
        }

       
        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(ApiResponse<object>.FailResponse("Invalid Id"));

                await _userService.UpdateUserAsync(new UpdateUserDto
                {
                    Id = id,
                    IsActive = false
                });

                return Ok(ApiResponse<object>.SuccessResponse(null!, "User deactivated successfully"));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning($"User with ID {id} not found during deactivation");
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning($"Validation error during user deactivation: {ex.Message}");
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating user with ID {id}");
                return StatusCode(500, ApiResponse<object>.FailResponse("Internal server error"));
            }
        }

        [HttpPost("import")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportUsers(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(ApiResponse<object>.FailResponse("Please upload a valid CSV file."));
            if (!file.FileName.EndsWith(".csv")) return BadRequest(ApiResponse<object>.FailResponse("Only .csv files are supported."));

            using var stream = file.OpenReadStream();
            var importedCount = await _userService.ImportUsersFromCsvAsync(stream);

            return Ok(ApiResponse<int>.SuccessResponse(importedCount, $"Successfully imported {importedCount} users."));
        }

        [HttpPut("{id}/preferences")]
        public async Task<IActionResult> UpdatePreferences(int id, [FromBody] NotificationPreferencesDto dto)
        {
            try
            {
                if (id <= 0) return BadRequest(ApiResponse<object>.FailResponse("Invalid Id"));

                // Users can only update their own preferences, unless they are Admin
                var currentUserIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                
                if (currentUserIdStr != id.ToString() && role != "Admin")
                {
                    return StatusCode(403, ApiResponse<object>.FailResponse("You can only update your own preferences."));
                }

                await _userService.UpdatePreferencesAsync(id, dto);
                return Ok(ApiResponse<object>.SuccessResponse(null!, "Preferences updated successfully."));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating preferences for user {id}");
                return StatusCode(500, ApiResponse<object>.FailResponse("Internal server error"));
            }
        }

        [HttpPut("{id}/profile")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileDto dto)
        {
            try
            {
                if (id <= 0) return BadRequest(ApiResponse<object>.FailResponse("Invalid Id"));

                var currentUserIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                if (currentUserIdStr != id.ToString() && role != "Admin")
                {
                    return StatusCode(403, ApiResponse<object>.FailResponse("You can only update your own profile."));
                }

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
                    return BadRequest(ApiResponse<object>.FailResponse($"Validation failed: {errors}"));
                }

                await _userService.UpdateProfileAsync(id, dto);
                return Ok(ApiResponse<object>.SuccessResponse(null!, "Profile updated successfully."));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating profile for user {id}");
                return StatusCode(500, ApiResponse<object>.FailResponse("Internal server error"));
            }
        }
    }
}
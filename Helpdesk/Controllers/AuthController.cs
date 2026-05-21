using Helpdesk.DTOs;
using Helpdesk.Interfaces;
using Helpdesk.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]                
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.FailResponse(
                    string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage))));

            var tokenModel = await _authService.LoginAsync(dto);

            if (tokenModel == null)
                return Unauthorized(ApiResponse<object>.FailResponse("Invalid email or password"));

            var response = new LoginResponseDto
            {
                Token = tokenModel.AccessToken,
                RefreshToken = tokenModel.RefreshToken
            };

            return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response, "Login successful"));
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenModelDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tokenModel = await _authService.RefreshTokenAsync(dto);
                return Ok(ApiResponse<TokenModelDto>.SuccessResponse(tokenModel, "Token refreshed successfully"));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [Authorize]
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke()
        {
            var userEmailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email);
            if (userEmailClaim == null)
                return Unauthorized(ApiResponse<object>.FailResponse("User not authorized"));

            await _authService.RevokeTokenAsync(userEmailClaim.Value);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Token revoked successfully"));
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(ApiResponse<object>.FailResponse("User not authorized"));

            try
            {
                await _authService.ChangePasswordAsync(userId, dto);
                return Ok(ApiResponse<object>.SuccessResponse(null, "Password changed successfully."));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailResponse(ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _authService.ForgotPasswordAsync(dto);
            // Always return Ok to prevent email enumeration
            return Ok(ApiResponse<object>.SuccessResponse(null, "If an account with that email exists, a reset link has been sent."));
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _authService.ResetPasswordAsync(dto);
                return Ok(ApiResponse<object>.SuccessResponse(null, "Password reset successfully. You can now login."));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<object>.FailResponse(ex.Message));
            }
        }
    }
}
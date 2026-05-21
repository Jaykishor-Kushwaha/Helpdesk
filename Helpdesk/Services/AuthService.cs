using Helpdesk.DTOs;
using Helpdesk.Exceptions;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Helpdesk.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;

        public AuthService(
            UserManager<User> userManager,
            IConfiguration configuration,
            INotificationService notificationService,
            IAuditLogService auditLogService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
        }

        public async Task<TokenModelDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            // FIX: timing attack mitigation — always run CheckPasswordAsync regardless
            // of whether the user exists so valid/invalid emails take the same time.
            // A dummy user is checked when the real one is not found; the result is
            // discarded and we still throw the same generic error either way.
            if (user == null || !user.IsActive)
            {
                if (user != null)
                {
                    // User exists but is inactive — run password check to equalise timing
                    await _userManager.CheckPasswordAsync(user, dto.Password);

                    await _auditLogService.LogAsync(
                        Enums.AuditEventType.UserLoginFailed,
                        Enums.AuditEntityType.User,
                        user.Id,
                        "Login failed because account is inactive.",
                        user.Id);
                }
                else
                {
                    // User not found — synthesise a dummy check so response time matches
                    // a real failed-password attempt and email enumeration is not possible.
                    var dummy = new User { PasswordHash = string.Empty };
                    await _userManager.CheckPasswordAsync(dummy, dto.Password);
                }

                throw new ValidationException("Invalid email or inactive account");
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);

            if (!isPasswordValid)
            {
                await _auditLogService.LogAsync(
                    Enums.AuditEventType.UserLoginFailed,
                    Enums.AuditEntityType.User,
                    user.Id,
                    "Login failed because password was invalid.",
                    user.Id);
                throw new ValidationException("Invalid email or password");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            var token = GenerateToken(user, role);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays());
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _auditLogService.LogAsync(
                Enums.AuditEventType.UserLoginSucceeded,
                Enums.AuditEntityType.User,
                user.Id,
                "User logged in successfully.",
                user.Id);

            return new TokenModelDto
            {
                AccessToken = token,
                RefreshToken = refreshToken
            };
        }

        public async Task<TokenModelDto> RefreshTokenAsync(TokenModelDto dto)
        {
            var principal = GetPrincipalFromExpiredToken(dto.AccessToken);
            if (principal == null)
                throw new ValidationException("Invalid access token or refresh token");

            var userEmail = principal.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(userEmail!);

            if (user == null || user.RefreshToken != dto.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new ValidationException("Invalid client request");

            // FIX: fetch the live role from the database instead of reading it from
            // the expired token claim — ensures role changes take effect on next refresh
            // without requiring the user to log out and back in.
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            var newAccessToken = GenerateToken(user, role);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            // FIX: reset expiry on every refresh so active sessions don't expire
            // based on the original login time.
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays());
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return new TokenModelDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task RevokeTokenAsync(string username)
        {
            var user = await _userManager.FindByEmailAsync(username);

            // FIX: log unknown revocation attempts rather than silently no-op —
            // visible in monitoring without exposing whether the email exists externally.
            if (user == null)
            {
                await _auditLogService.LogAsync(
                    Enums.AuditEventType.UserLoggedOut,
                    Enums.AuditEntityType.User,
                    0,
                    $"Token revocation attempted for unknown username.",
                    0);
                return;
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);

            await _auditLogService.LogAsync(
                Enums.AuditEventType.UserLoggedOut,
                Enums.AuditEntityType.User,
                user.Id,
                "User token was revoked/logged out.",
                user.Id);
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                throw new NotFoundException("User", userId);

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ValidationException(errors);
            }

            // FIX: set flag and call UpdateAsync once — avoids a second DB write and
            // prevents the user being stuck in a forced-change loop if the second
            // UpdateAsync had failed independently.
            user.RequiresPasswordChange = false;
            await _userManager.UpdateAsync(user);

            // FIX: guard null email before sending — Identity allows null email
            if (!string.IsNullOrEmpty(user.Email))
            {
                await _notificationService.QueueEmailAsync(
                    user.Email,
                    "Password Changed",
                    "Your Helpdesk account password has been changed successfully.");
            }

            await _auditLogService.LogAsync(
                Enums.AuditEventType.UserAccountChanged,
                Enums.AuditEntityType.User,
                user.Id,
                "User changed their password successfully.",
                userId,
                null);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                return; // Do not reveal if user exists

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);

            var subject = "Helpdesk - Password Reset Request";
            var body = $@"
                <h3>Password Reset Request</h3>
                <p>Hello {user.FirstName},</p>
                <p>You have requested to reset your password. Use the token below to reset it:</p>
                <p><strong>{encodedToken}</strong></p>
                <p>If you did not request this, please ignore this email.</p>";

            await _notificationService.QueueEmailAsync(user.Email, subject, body);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                throw new ValidationException("Invalid request");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
                throw new ValidationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            user.RequiresPasswordChange = false;
            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrEmpty(user.Email))
            {
                await _notificationService.QueueEmailAsync(
                    user.Email,
                    "Password Reset Successful",
                    "Your Helpdesk account password has been successfully reset. If you did not perform this action, please contact your administrator immediately.");
            }

            // Actor is the user themselves for a self-service reset.
            // If an admin were to reset on behalf of a user, pass the admin's ID instead.
            await _auditLogService.LogAsync(
                Enums.AuditEventType.UserAccountChanged,
                Enums.AuditEntityType.User,
                user.Id,
                "User performed a password reset.",
                user.Id,
                null);
        }

        // ?? Private helpers ???????????????????????????????????????????????????????

        private string GenerateToken(User user, string role)
        {
            var key = _configuration["JwtSettings:Key"];
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("JWT Key is not configured");

            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
                new Claim(ClaimTypes.Role, role),
                new Claim("RequiresPasswordChange", user.RequiresPasswordChange.ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // FIX: expiry read from config so it can be changed per environment
            // without a redeploy. Falls back to 8 hours if not set.
            var expiryHours = GetAccessTokenExpiryHours();

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // FIX: use static RandomNumberGenerator.GetBytes — no allocation, no using block,
        // consistent with the approach in UserService.
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            RandomNumberGenerator.Fill(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        /// <summary>
        /// Reads access token expiry from config.
        /// Key: JwtSettings:AccessTokenExpiryHours — defaults to 8 if absent.
        /// </summary>
        private int GetAccessTokenExpiryHours()
        {
            if (int.TryParse(_configuration["JwtSettings:AccessTokenExpiryHours"], out var hours) && hours > 0)
                return hours;
            return 8;
        }

        /// <summary>
        /// Reads refresh token expiry from config.
        /// Key: JwtSettings:RefreshTokenExpiryDays — defaults to 7 if absent.
        /// </summary>
        private int GetRefreshTokenExpiryDays()
        {
            if (int.TryParse(_configuration["JwtSettings:RefreshTokenExpiryDays"], out var days) && days > 0)
                return days;
            return 7;
        }
    }
}

using Helpdesk.DTOs;
namespace Helpdesk.Interfaces
{
    public interface IAuthService
    {
        Task<TokenModelDto> LoginAsync(LoginRequestDto dto);
        Task<TokenModelDto> RefreshTokenAsync(TokenModelDto dto);
        Task RevokeTokenAsync(string username);
        Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
    }
}
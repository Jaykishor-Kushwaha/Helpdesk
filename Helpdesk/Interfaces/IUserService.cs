

using Helpdesk.DTOs;

namespace Helpdesk.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto> GetUserByIdAsync(int id); 
        Task<UserResponseDto> CreateUserAsync(CreateUserDto dto);
        Task UpdateUserAsync(UpdateUserDto dto);
        Task DeleteUserAsync(int id); 
        Task<int> ImportUsersFromCsvAsync(Stream csvStream);
        Task UpdatePreferencesAsync(int userId, NotificationPreferencesDto dto);
        Task UpdateProfileAsync(int id, UpdateProfileDto dto);
    }
}
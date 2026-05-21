using Helpdesk.Enums;
namespace Helpdesk.Interfaces
{
    public interface ICurrentUserService
    {
        int UserId { get; }
        string Role { get; }
        UserRole UserRole { get; }
        string FullName { get; }
        string Email { get; }
        string? IpAddress { get; }
    }
}
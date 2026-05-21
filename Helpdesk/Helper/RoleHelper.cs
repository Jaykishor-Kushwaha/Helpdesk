using Helpdesk.Enums;

namespace Helpdesk.Helper
{
    public static class RoleHelper
    {
        // ✅ Enum → String (for Identity)
        public static string GetRoleName(UserRole role)
        {
            return Enum.GetName(typeof(UserRole), role)!;
        }

        // ✅ String → Enum (safe + case-insensitive)
        public static UserRole GetRoleEnum(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role cannot be null or empty");

            if (Enum.TryParse<UserRole>(role, true, out var parsedRole))
                return parsedRole;

            throw new ArgumentException($"Invalid role: {role}");
        }
    }
}
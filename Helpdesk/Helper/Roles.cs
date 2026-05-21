using Helpdesk.Enums;

namespace Helpdesk.Helper
{
    public static class Roles
    {
        public const string Admin = nameof(UserRole.Admin);
        public const string Agent = nameof(UserRole.Agent);
        public const string User = nameof(UserRole.User);
        public const string DepartmentHead = nameof(UserRole.DepartmentHead);

        public const string AdminOnly = nameof(UserRole.Admin);
        public const string AgentOnly = nameof(UserRole.Agent);
        public const string UserOnly = nameof(UserRole.User);
        public const string DepartmentHeadOnly = nameof(UserRole.DepartmentHead);

        public const string AdminAndAgent = $"{nameof(UserRole.Admin)},{nameof(UserRole.Agent)}";
        public const string AdminAndUser = $"{nameof(UserRole.Admin)},{nameof(UserRole.User)}";
        public const string AdminAndDepartmentHead = $"{nameof(UserRole.Admin)},{nameof(UserRole.DepartmentHead)}";
        public const string All = $"{nameof(UserRole.Admin)},{nameof(UserRole.Agent)},{nameof(UserRole.User)},{nameof(UserRole.DepartmentHead)}";
    }
}

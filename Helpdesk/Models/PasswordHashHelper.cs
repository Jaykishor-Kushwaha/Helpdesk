using Helpdesk.Models;
using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Helper
{
    public class PasswordHashHelper  
    {
        public static string GenerateHash(string password)
        {
            var hasher = new PasswordHasher<User>();
            var user = new User();
            return hasher.HashPassword(user, password);
        }

        public static bool VerifyPassword(string hashedPassword, string inputPassword)
        {
            var hasher = new PasswordHasher<User>();
            var user = new User();

            var result = hasher.VerifyHashedPassword(user, hashedPassword, inputPassword);

            return result == PasswordVerificationResult.Success;
        }
    }
}
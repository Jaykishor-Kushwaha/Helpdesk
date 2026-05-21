using Helpdesk.Enums;
using Helpdesk.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(
            AppDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            string adminEmail,
            string password)
        {
            // ✅ Apply migrations
            await context.Database.MigrateAsync();

            // ✅ Ensure roles exist
            await EnsureRoleAsync(roleManager, nameof(UserRole.Admin));
            await EnsureRoleAsync(roleManager, nameof(UserRole.Agent));
            await EnsureRoleAsync(roleManager, nameof(UserRole.User));
            await EnsureRoleAsync(roleManager, nameof(UserRole.DepartmentHead));

            // ✅ Create admin if not exists
            var existingUser = await userManager.FindByEmailAsync(adminEmail);

            if (existingUser == null)
            {
                var admin = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Jaykishor",
                    LastName = "Kushwaha",
                    IsActive = true
                };

                var result = await userManager.CreateAsync(admin, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create admin user: {errors}");
                }

                await userManager.AddToRoleAsync(admin, nameof(UserRole.Admin));
            }

            // ✅ Seed categories
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Hardware" },
                    new Category { Name = "Software" },
                    new Category { Name = "Network" },
                    new Category { Name = "HR" },
                    new Category { Name = "Other" }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // ✅ Seed Recurring Templates
            if (!await context.RecurringTemplates.AnyAsync())
            {
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
                var softwareCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Software") ?? await context.Categories.FirstAsync();
                var networkCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Network") ?? await context.Categories.FirstAsync();
                var otherCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Other") ?? await context.Categories.FirstAsync();

                var templates = new List<RecurringTemplate>
                {
                    new RecurringTemplate
                    {
                        Name = "Weekly Server Maintenance Checks",
                        TicketTitle = "[Routine] Server Maintenance & Updates",
                        Description = "Perform routine checks on main production servers, apply security patches, and verify backups.",
                        CategoryId = networkCategory.Id,
                        Priority = TicketPriority.High,
                        RaiseOnBehalfOfId = adminUser?.Id,
                        CronExpression = "0 9 * * 1", // Every Monday at 9 AM
                        StartDate = DateTime.UtcNow,
                        IsActive = true
                    },
                    new RecurringTemplate
                    {
                        Name = "Quarterly IT Asset Audits",
                        TicketTitle = "[Audit] Quarterly Hardware & Asset Review",
                        Description = "Conduct a full audit of all company-issued laptops, monitors, and peripherals. Update the asset management system.",
                        CategoryId = otherCategory.Id,
                        Priority = TicketPriority.Medium,
                        RaiseOnBehalfOfId = adminUser?.Id,
                        CronExpression = "0 9 1 */3 *", // 1st day of every 3rd month at 9 AM
                        StartDate = DateTime.UtcNow,
                        IsActive = true
                    },
                    new RecurringTemplate
                    {
                        Name = "Weekly Password Expiry Reviews",
                        TicketTitle = "[Security] Review Expiring Passwords",
                        Description = "Check Active Directory for users whose passwords will expire in the next 7 days and send them advance reminders.",
                        CategoryId = softwareCategory.Id,
                        Priority = TicketPriority.Medium,
                        RaiseOnBehalfOfId = adminUser?.Id,
                        CronExpression = "0 10 * * 5", // Every Friday at 10 AM
                        StartDate = DateTime.UtcNow,
                        IsActive = true
                    }
                };

                await context.RecurringTemplates.AddRangeAsync(templates);
                await context.SaveChangesAsync();
            }
        }

        // 🔹 Helper method (clean code)
        private static async Task EnsureRoleAsync(
            RoleManager<IdentityRole<int>> roleManager,
            string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }
        }
    }
}
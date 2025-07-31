using Microsoft.AspNetCore.Identity;

namespace APIMonitor.server.Identity.Seeding;

public static class RoleFactory
{
    public static async Task SeedRoles(RoleManager<IdentityRole<int>> roleManager)
    {
        string[] roles = { "Admin", "User" };

        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
    }
}
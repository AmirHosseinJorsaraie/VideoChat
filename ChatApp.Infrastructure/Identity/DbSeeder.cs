using ChatApp.Core.Enums;
using Microsoft.AspNetCore.Identity;

namespace ChatApp.Infrastructure.Identity;

public static class DbSeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}

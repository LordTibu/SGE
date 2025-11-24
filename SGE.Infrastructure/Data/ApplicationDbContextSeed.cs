using Microsoft.AspNetCore.Identity;
using SGE.Core.Entities;

namespace SGE.Infrastructure.Data;

/// <summary>
/// Provides methods for seeding initial data into the application database.
/// </summary>
public static class ApplicationDbContextSeed
{
    /// <summary>
    /// Seeds the database with default roles and an admin user if they don't already exist.
    /// </summary>
    /// <param name="userManager">The user manager for creating and managing users.</param>
    /// <param name="roleManager">The role manager for creating and managing roles.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Créer les rôles s'ils n'existent pas
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        if (!await roleManager.RoleExistsAsync("Manager"))
            await roleManager.CreateAsync(new IdentityRole("Manager"));

        if (!await roleManager.RoleExistsAsync("User"))
            await roleManager.CreateAsync(new IdentityRole("User"));

        // Créer l'utilisateur admin s'il n'existe pas
        var adminEmail = "admin@example.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                FirstName = "Super",
                LastName = "Admin",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}


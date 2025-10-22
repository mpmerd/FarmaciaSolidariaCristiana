using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created
            await context.Database.MigrateAsync();

            // Create roles
            string[] roleNames = { "Admin", "Farmaceutico", "Viewer", "ViewerPublic" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create default admin user
            var adminEmail = "admin@example.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new IdentityUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newAdmin, "doqkox-gadqud-niJho0");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
            else
            {
                // Update existing admin password
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                await userManager.ResetPasswordAsync(adminUser, token, "doqkox-gadqud-niJho0");
            }

            // Seed Sponsors
            if (!await context.Sponsors.AnyAsync())
            {
                var sponsors = new List<Sponsor>
                {
                    new Sponsor { Name = "ACAA", Description = "Asociación Cubana de Artesanos Artistas", LogoPath = "/images/sponsors/acaa.png", IsActive = true, DisplayOrder = 1, CreatedDate = DateTime.Now },
                    new Sponsor { Name = "Adriano Solidaire", Description = "Adriano Solidario", LogoPath = "/images/sponsors/adranosolidaire.png", IsActive = true, DisplayOrder = 2, CreatedDate = DateTime.Now },
                    new Sponsor { Name = "Apotheek", Description = "Apotheek Peeters Herent, Bélgica", LogoPath = "/images/sponsors/apotheek.png", IsActive = true, DisplayOrder = 3, CreatedDate = DateTime.Now },
                    new Sponsor { Name = "HSF", Description = "Hospital Sans Frontière", LogoPath = "/images/sponsors/hsf.JPG", IsActive = true, DisplayOrder = 4, CreatedDate = DateTime.Now },
                    new Sponsor { Name = "Farmacia Janeiro", Description = "Farmacia Janeiro, Portugal", LogoPath = "/images/sponsors/janeiro.png", IsActive = true, DisplayOrder = 5, CreatedDate = DateTime.Now },
                    new Sponsor { Name = "Sutures Medical", Description = "Aip Medical, Bélgica", LogoPath = "/images/sponsors/suturesmedical.png", IsActive = true, DisplayOrder = 6, CreatedDate = DateTime.Now }
                };
                
                context.Sponsors.AddRange(sponsors);
                await context.SaveChangesAsync();
            }
        }
    }
}

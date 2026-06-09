using LicensingAPI.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LicensingAPI.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply any pending migrations automatically (optional)
            await context.Database.MigrateAsync();

            await SeedRolesAsync(roleManager);
            await SeedAdminUserAsync(userManager);
            //await SeedProductsAsync(context);
        }

        // ── 1. Seed Roles ─────────────────────────────────────────
        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = ["Admin", "User", "Manager"];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        // ── 2. Seed Admin User ────────────────────────────────────
        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            const string adminEmail = "admin@spice.com";
            const string adminPassword = "Admin@4321";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Admin",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }

        // ── 3. Seed Initial Product ───────────────────────────────
        //private static async Task SeedProductsAsync(ApplicationDbContext context)
        //{
        //    // Only seed if table is empty
        //    if (context.Products.Any()) return;

        //    var product = new Product
        //    {
        //        KeygenProductId = "your-keygen-product-uuid-here",
        //        Name = "My Software",
        //        Code = "MY_SOFTWARE",
        //        Version = 1,
        //        Description = "Main software product",
        //        CreatedAt = DateTime.UtcNow,
        //        UpdatedAt = DateTime.UtcNow
        //    };

        //    context.Products.Add(product);
        //    await context.SaveChangesAsync();
        //}
    }
}

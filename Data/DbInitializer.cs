using HDKTech.Data.Seeds;
using HDKTech.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data
{
    /// <summary>
    /// Điểm khởi tạo duy nhất — gọi các Seed file theo đúng thứ tự phụ thuộc FK.
    /// Thứ tự: Migrate → Layer1 → Layer2 → Layer3
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            IServiceProvider services,
            HDKTechContext context)
        {
            // ── Step 0: Migrate ─────────────────────────────────────────
            await context.Database.MigrateAsync();

            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // ── Layer 1: Master data (no foreign key deps) ───────────────
            await UserSeed.SeedAsync(userManager, roleManager);
            await BrandSeed.SeedAsync(context);
            await CategorySeed.SeedAsync(context);

            // ── Layer 2: Depends on Layer 1 ─────────────────────────────
            await ProductSeed.SeedAsync(context);          // FK → Brand, Category
            await BannerSeed.SeedBannersAsync(context);  // Independent
            await PromotionSeed.SeedAsync(context);        // Independent

            // ── Layer 3: Depends on Layer 1 + 2 ─────────────────────────
            await OrderSeed.SeedAsync(context);            // FK → Users, Products
            await ReviewSeed.SeedAsync(context);           // FK → Users, Products
            await SystemLogSeed.SeedAsync(context);        // FK → Users
        }
    }
}


using HDKTech.Data.Seeds;
using HDKTech.Models;
using HDKTech.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data
{
    /// <summary>
    /// Điểm khởi tạo duy nhất — gọi các Seed file theo đúng thứ tự phụ thuộc FK.
    ///
    /// Thứ tự (sau khi hợp nhất Identity):
    ///   Migrate
    ///   ├─ Layer 0: Authz      → IdentityRoleSeed (Identity Roles + RoleClaims)
    ///   │                        WarrantyPolicySeed
    ///   ├─ Layer 1: Identity   → UserSeed (tạo user + AddToRoleAsync)
    ///   ├─ Layer 2: Metadata   → BrandSeed, CategorySeed
    ///   ├─ Layer 3: Product    → ProductSeed → ProductVariantSeed + Inventory + StockMovement
    ///   ├─ Layer 4: Marketing  → BannerSeed, PromotionSeed (+ PromotionProducts)
    ///   ├─ Layer 5: Tags       → ProductTagSeed
    ///   └─ Layer 6: Tx/Audit   → OrderSeed → ReviewSeed → SystemLogSeed
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            IServiceProvider services,
            HDKTechContext context)
        {
            await context.Database.MigrateAsync();

            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            // ── Layer 0: Authorization foundation (Identity) ───────
            await IdentityRoleSeed.SeedAsync(services);
            await WarrantyPolicySeed.SeedAsync(context);

            // ── Layer 1: Identity users ────────────────────────────
            await UserSeed.SeedAsync(userManager, context);

            // ── Layer 2: Metadata (no FK deps) ─────────────────────
            await BrandSeed.SeedAsync(context);
            await CategorySeed.SeedAsync(context);

            // ── Layer 3: Product stack ─────────────────────────────
            await ProductSeed.SeedAsync(context);
            await ProductVariantSeed.SeedAsync(context);   // +Inventory +StockMovement

            // ── Layer 4: Marketing content ─────────────────────────
            await BannerSeed.SeedBannersAsync(context);
            await PromotionSeed.SeedAsync(context);        // +PromotionProducts

            // ── Layer 5: Tag / liên kết sản phẩm ───────────────────
            await ProductTagSeed.SeedAsync(context);

            // ── Layer 6: Transactional + audit ─────────────────────
            await OrderSeed.SeedAsync(context);
            await ReviewSeed.SeedAsync(context);
            await SystemLogSeed.SeedAsync(context);

            // ── Preload category cache sau khi seed xong ───────────
            var categoryCache = services.GetRequiredService<ICategoryCacheService>();
            await categoryCache.LoadCacheAsync();
        }
    }
}

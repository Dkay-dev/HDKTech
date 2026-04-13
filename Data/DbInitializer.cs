using HDKTech.Areas.Admin.Models;
using HDKTech.Areas.Identity.Data;
using HDKTech.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data
{
    /// <summary>
    /// Điểm khởi tạo dữ liệu duy nhất của ứng dụng.
    /// Gom toàn bộ logic Migrate + Seed vào một nơi để dễ quản lý.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, HDKTechContext context)
        {
            // ─── Step 1: Migrate DB ──────────────────────────────────────
            await context.Database.MigrateAsync();

            // ─── Step 2: Seed roles, admin user, brands, categories ──────
            await DataSeed.KhoiTaoDuLieuMacDinh(services);

            // ─── Step 3: Seed products ───────────────────────────────────
            await DataSeedProducts.SeedProductsWithSpecs(context);

            // ─── Step 4: Seed banners ────────────────────────────────────
            await BannerSeeder.SeedBannersAsync(context);
        }
    }
}


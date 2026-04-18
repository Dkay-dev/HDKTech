using HDKTech.Areas.Admin.Models;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    /// <summary>
    /// Seed Promotion + PromotionProduct (bảng nối scope).
    ///
    /// Migration so với bản cũ:
    ///  - ApplicableCategory (string) ─▶ AppliesToAll + PromotionProduct(ScopeType=Category).
    ///  - PromotionType (string) ─▶ enum PromotionType.
    ///  - Status (string) ─▶ enum PromotionStatus.
    ///  - IsFlashSale / FlashSalePrice trên Product ─▶ 1 Promotion(PromotionType.FlashSale)
    ///    + dòng PromotionProduct(ScopeType=Product) cho từng sản phẩm flash.
    /// </summary>
    public static class PromotionSeed
    {
        public static async Task SeedAsync(HDKTechContext context)
        {
            if (await context.Promotions.AnyAsync()) return;

            using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Promotions] ON");

                var now = DateTime.Now;

                // ── 1. Promotions (header) ───────────────────────────
                var promotions = new[]
                {
                    new Promotion
                    {
                        Id             = 1,
                        CampaignName   = "Siêu Sale HDKTech 12.12",
                        Description    = "Giảm sốc dịp 12/12, áp dụng toàn bộ Laptop và Laptop Gaming",
                        PromotionType  = PromotionType.Percentage,
                        Value          = 12,              // 12%
                        MaxDiscountAmount = 3_000_000,
                        PromoCode      = "HDK1212",
                        UsageCount     = 47,
                        StartDate      = now.AddDays(-5),
                        EndDate        = now.AddDays(5),
                        IsActive       = true,
                        Status         = PromotionStatus.Running,
                        AppliesToAll   = false,           // scope = Category 1 & 2
                        CreatedAt      = now.AddDays(-10)
                    },
                    new Promotion
                    {
                        Id             = 2,
                        CampaignName   = "Ưu đãi khách hàng Liên Chiểu",
                        Description    = "Giảm giá đặc biệt cho khách hàng tại quận Liên Chiểu, Hòa Khánh",
                        PromotionType  = PromotionType.FixedAmount,
                        Value          = 500_000,
                        MinOrderAmount = 5_000_000,
                        PromoCode      = "LIENCHIEUDN",
                        UsageCount     = 12,
                        StartDate      = now.AddDays(-2),
                        EndDate        = now.AddDays(28),
                        IsActive       = true,
                        Status         = PromotionStatus.Running,
                        AppliesToAll   = true,
                        CreatedAt      = now.AddDays(-5)
                    },
                    new Promotion
                    {
                        Id             = 3,
                        CampaignName   = "Black Friday 2024 - Trước Tết",
                        Description    = "Giảm mạnh toàn bộ linh kiện Main/CPU/VGA trước Tết Nguyên Đán",
                        PromotionType  = PromotionType.Percentage,
                        Value          = 15,
                        MaxDiscountAmount = 10_000_000,
                        PromoCode      = "BLACKFRIDAY24",
                        UsageCount     = 0,
                        StartDate      = now.AddDays(10),
                        EndDate        = now.AddDays(20),
                        IsActive       = true,
                        Status         = PromotionStatus.Scheduled,
                        AppliesToAll   = false,            // scope = Category MainCpuVga
                        CreatedAt      = now.AddDays(-1)
                    },
                    new Promotion
                    {
                        Id             = 4,
                        CampaignName   = "Miễn phí vận chuyển tháng 10",
                        Description    = "Free ship cho đơn hàng từ 3 triệu khu vực nội thành Đà Nẵng",
                        PromotionType  = PromotionType.FreeShip,
                        Value          = 0,                // 0 = miễn toàn bộ phí ship
                        MinOrderAmount = 3_000_000,
                        PromoCode      = "FREESHIP10",
                        UsageCount     = 203,
                        StartDate      = now.AddDays(-40),
                        EndDate        = now.AddDays(-10),
                        IsActive       = false,
                        Status         = PromotionStatus.Ended,
                        AppliesToAll   = true,
                        CreatedAt      = now.AddDays(-45)
                    },
                    new Promotion
                    {
                        Id             = 5,
                        CampaignName   = "Khai trương CN Hải Châu - Giảm 20%",
                        Description    = "Mừng khai trương chi nhánh Hải Châu, áp dụng mọi sản phẩm",
                        PromotionType  = PromotionType.Percentage,
                        Value          = 20,
                        MaxDiscountAmount = 5_000_000,
                        PromoCode      = "KHAITUONGDN",
                        MaxUsageCount  = 100,
                        MaxUsagePerUser = 1,
                        UsageCount     = 0,
                        StartDate      = now.AddDays(30),
                        EndDate        = now.AddDays(37),
                        IsActive       = true,
                        Status         = PromotionStatus.Scheduled,
                        AppliesToAll   = true,
                        CreatedAt      = now
                    },

                    // ── Flash Sale (tách từ IsFlashSale/FlashSalePrice trên Product cũ) ──
                    new Promotion
                    {
                        Id             = 6,
                        CampaignName   = "Flash Sale Gaming Laptop Cuối Tuần",
                        Description    = "Giảm sâu ASUS ROG & MSI flagship trong 3 ngày cuối tuần",
                        PromotionType  = PromotionType.FlashSale,
                        Value          = 10,               // giảm 10% niêm yết (ưu tiên hiển thị)
                        PromoCode      = null,             // auto-apply, không cần nhập
                        UsageCount     = 23,
                        StartDate      = now.AddDays(-1),
                        EndDate        = now.AddDays(2),
                        IsActive       = true,
                        Status         = PromotionStatus.Running,
                        AppliesToAll   = false,
                        CreatedAt      = now.AddDays(-2)
                    },
                    new Promotion
                    {
                        Id             = 7,
                        CampaignName   = "Flash Sale Phụ Kiện 99K-990K",
                        Description    = "Chuột/bàn phím/tai nghe giảm giá kịch sàn mỗi 12h",
                        PromotionType  = PromotionType.FlashSale,
                        Value          = 15,
                        PromoCode      = null,
                        UsageCount     = 8,
                        StartDate      = now.AddHours(-12),
                        EndDate        = now.AddHours(12),
                        IsActive       = true,
                        Status         = PromotionStatus.Running,
                        AppliesToAll   = false,
                        CreatedAt      = now.AddDays(-1)
                    },
                };

                await context.Promotions.AddRangeAsync(promotions);
                await context.SaveChangesAsync();
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Promotions] OFF");

                // ── 2. PromotionProducts (bảng nối scope) ───────────
                // Với AppliesToAll = true thì không cần dòng nào cả.
                var junctions = new List<PromotionProduct>
                {
                    // Promo 1 — Laptop + Laptop Gaming (scope = Category)
                    new() { PromotionId = 1, ScopeType = PromotionScopeType.Category, CategoryId = SeedConstants.CatLaptop },
                    new() { PromotionId = 1, ScopeType = PromotionScopeType.Category, CategoryId = SeedConstants.CatLaptopGaming },

                    // Promo 3 — Main/CPU/VGA (scope = Category)
                    new() { PromotionId = 3, ScopeType = PromotionScopeType.Category, CategoryId = SeedConstants.CatMainCpuVga },

                    // Promo 6 — Flash Sale Gaming Laptop (scope = Product, 3 sản phẩm cụ thể)
                    new() { PromotionId = 6, ScopeType = PromotionScopeType.Product, ProductId = 11 },  // ASUS ROG Strix G16
                    new() { PromotionId = 6, ScopeType = PromotionScopeType.Product, ProductId = 14 },  // Lenovo Legion Pro 7i
                    new() { PromotionId = 6, ScopeType = PromotionScopeType.Product, ProductId = 16 },  // MSI Raider GE78 HX

                    // Promo 7 — Flash Sale Phụ kiện (scope = Category, gộp 3 category peripheral)
                    new() { PromotionId = 7, ScopeType = PromotionScopeType.Category, CategoryId = SeedConstants.CatChuot },
                    new() { PromotionId = 7, ScopeType = PromotionScopeType.Category, CategoryId = SeedConstants.CatBanPhim },
                    new() { PromotionId = 7, ScopeType = PromotionScopeType.Category, CategoryId = SeedConstants.CatTaiNghe },
                };

                await context.PromotionProducts.AddRangeAsync(junctions);
                await context.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}

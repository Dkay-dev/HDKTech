using HDKTech.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    public static class PromotionSeed
    {
        public static async Task SeedAsync(HDKTechContext context)
        {
            if (await context.Promotions.AnyAsync()) return;

            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Promotion] ON");

                    var now = DateTime.Now;

                    var promotions = new[]
                    {
                        new Promotion
                        {
                            Id             = 1,
                            CampaignName   = "Siêu Sale HDKTech 12.12",
                            Description    = "Giảm sốc dịp 12/12, áp dụng toàn bộ Laptop và PC Gaming",
                            ApplicableCategory = "Laptop, Laptop Gaming",
                            StartDate      = now.AddDays(-5),
                            EndDate        = now.AddDays(5),
                            PromotionType  = "Percentage",
                            Value          = 12,
                            PromoCode      = "HDK1212",
                            UsageCount     = 47,
                            IsActive       = true,
                            Status         = "Running",
                            CreatedAt      = now.AddDays(-10)
                        },
                        new Promotion
                        {
                            Id             = 2,
                            CampaignName   = "Ưu đãi khách hàng Liên Chiểu",
                            Description    = "Giảm giá đặc biệt cho khách hàng tại quận Liên Chiểu, Hòa Khánh",
                            ApplicableCategory = "Tất cả sản phẩm",
                            StartDate      = now.AddDays(-2),
                            EndDate        = now.AddDays(28),
                            PromotionType  = "FixedAmount",
                            Value          = 500_000,
                            PromoCode      = "LIENCHIEUDN",
                            UsageCount     = 12,
                            IsActive       = true,
                            Status         = "Running",
                            CreatedAt      = now.AddDays(-5)
                        },
                        new Promotion
                        {
                            Id             = 3,
                            CampaignName   = "Black Friday 2024 - Trước Tết",
                            Description    = "Giảm mạnh toàn bộ GPU RTX Series trước dịp Tết Nguyên Đán",
                            ApplicableCategory = "Main, CPU, VGA",
                            StartDate      = now.AddDays(10),
                            EndDate        = now.AddDays(20),
                            PromotionType  = "Percentage",
                            Value          = 15,
                            PromoCode      = "BLACKFRIDAY24",
                            UsageCount     = 0,
                            IsActive       = true,
                            Status         = "Scheduled",
                            CreatedAt      = now.AddDays(-1)
                        },
                        new Promotion
                        {
                            Id             = 4,
                            CampaignName   = "Miễn phí vận chuyển tháng 10",
                            Description    = "Free ship cho đơn hàng từ 3 triệu khu vực nội thành Đà Nẵng",
                            ApplicableCategory = "Tất cả sản phẩm",
                            StartDate      = now.AddDays(-40),
                            EndDate        = now.AddDays(-10),
                            PromotionType  = "FreeShip",
                            Value          = 0,
                            PromoCode      = "FREESHIP10",
                            UsageCount     = 203,
                            IsActive       = false,
                            Status         = "Ended",
                            CreatedAt      = now.AddDays(-45)
                        },
                        new Promotion
                        {
                            Id             = 5,
                            CampaignName   = "Khai trương CN Hải Châu - Giảm 20%",
                            Description    = "Mừng khai trương chi nhánh Hải Châu, áp dụng mọi sản phẩm",
                            ApplicableCategory = "Tất cả sản phẩm",
                            StartDate      = now.AddDays(30),
                            EndDate        = now.AddDays(37),
                            PromotionType  = "Percentage",
                            Value          = 20,
                            PromoCode      = "KHAITUONGDN",
                            MaxUsageCount  = 100,
                            UsageCount     = 0,
                            IsActive       = true,
                            Status         = "Scheduled",
                            CreatedAt      = now
                        },
                    };

                    await context.Promotions.AddRangeAsync(promotions);
                    await context.SaveChangesAsync();

                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Promotion] OFF");
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}
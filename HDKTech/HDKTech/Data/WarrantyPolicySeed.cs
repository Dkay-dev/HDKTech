using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    /// <summary>
    /// Seed 3 chính sách bảo hành mặc định để gán cho Product.
    /// </summary>
    public static class WarrantyPolicySeed
    {
        public static async Task SeedAsync(HDKTechContext context)
        {
            if (await context.WarrantyPolicies.AnyAsync()) return;

            using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [WarrantyPolicies] ON");

                var now = DateTime.Now;
                var policies = new[]
                {
                    new WarrantyPolicy
                    {
                        Id = SeedConstants.WarrantyStd24,
                        Code = "HDK-STD-24",
                        Name = "Bảo hành chính hãng 24 tháng",
                        DurationMonths = 24,
                        Coverage = "Tại hãng + Tại HDKTech",
                        Terms = "Bảo hành miễn phí lỗi do nhà sản xuất trong 24 tháng. " +
                                "Hỗ trợ 1 đổi 1 trong 15 ngày đầu nếu có lỗi nặng.",
                        Exclusions = "Rơi vỡ, vào nước, cháy nổ do sai điện áp, tự ý tháo máy.",
                        IsActive = true,
                        CreatedAt = now
                    },
                    new WarrantyPolicy
                    {
                        Id = SeedConstants.WarrantyStd12,
                        Code = "HDK-STD-12",
                        Name = "Bảo hành chính hãng 12 tháng",
                        DurationMonths = 12,
                        Coverage = "Tại hãng",
                        Terms = "Bảo hành 12 tháng với các phụ kiện và linh kiện nhỏ.",
                        Exclusions = "Rơi vỡ, vào nước, hao mòn tự nhiên.",
                        IsActive = true,
                        CreatedAt = now
                    },
                    new WarrantyPolicy
                    {
                        Id = SeedConstants.WarrantyNone,
                        Code = "HDK-NONE",
                        Name = "Không bảo hành",
                        DurationMonths = 0,
                        Coverage = "N/A",
                        Terms = "Sản phẩm phụ kiện/khuyến mãi — không áp dụng bảo hành.",
                        IsActive = true,
                        CreatedAt = now
                    }
                };

                await context.WarrantyPolicies.AddRangeAsync(policies);
                await context.SaveChangesAsync();

                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [WarrantyPolicies] OFF");
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

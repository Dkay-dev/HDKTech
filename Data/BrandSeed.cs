using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    public static class BrandSeed
    {
        public static async Task SeedAsync(HDKTechContext context)
        {
            if (await context.Brands.AnyAsync()) return;

            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Brands] ON");

                    var brands = new[]
                    {
                        new Brand { Id = SeedConstants.BrandAsus,       Name = "ASUS",         Description = "Tập đoàn công nghệ Đài Loan" },
                        new Brand { Id = SeedConstants.BrandMsi,        Name = "MSI",          Description = "Chuyên gaming và hiệu năng cao" },
                        new Brand { Id = SeedConstants.BrandGigabyte,   Name = "GIGABYTE",     Description = "Linh kiện máy tính chất lượng" },
                        new Brand { Id = SeedConstants.BrandLenovo,     Name = "LENOVO",       Description = "Hãng PC hàng đầu thế giới" },
                        new Brand { Id = SeedConstants.BrandAcer,       Name = "ACER",         Description = "Công nghệ cho mọi người" },
                        new Brand { Id = SeedConstants.BrandDell,       Name = "DELL",         Description = "Giải pháp doanh nghiệp & consumer" },
                        new Brand { Id = SeedConstants.BrandApple,      Name = "APPLE",        Description = "Think Different" },
                        new Brand { Id = SeedConstants.BrandIntel,      Name = "INTEL",        Description = "CPU & chipset hàng đầu" },
                        new Brand { Id = SeedConstants.BrandAmd,        Name = "AMD",          Description = "Ryzen & Radeon" },
                        new Brand { Id = SeedConstants.BrandNvidia,     Name = "NVIDIA",       Description = "GPU GeForce & CUDA" },
                        new Brand { Id = SeedConstants.BrandLogitech,   Name = "LOGITECH",     Description = "Chuột, bàn phím, webcam" },
                        new Brand { Id = SeedConstants.BrandRazer,      Name = "RAZER",        Description = "For Gamers, By Gamers" },
                        new Brand { Id = SeedConstants.BrandSamsung,    Name = "SAMSUNG",      Description = "Màn hình & SSD chất lượng" },
                        new Brand { Id = SeedConstants.BrandCorsair,    Name = "CORSAIR",      Description = "Gaming peripherals & PC components" },
                        new Brand { Id = SeedConstants.BrandKingston,   Name = "KINGSTON",     Description = "Bộ nhớ và lưu trữ tin cậy" },
                        new Brand { Id = SeedConstants.BrandNzxt,       Name = "NZXT",         Description = "Case & cooling cao cấp" },
                        new Brand { Id = SeedConstants.BrandLianLi,     Name = "LIAN LI",      Description = "Vỏ case premium nhôm nguyên khối" },
                        new Brand { Id = SeedConstants.BrandAkko,       Name = "AKKO",         Description = "Bàn phím cơ custom" },
                        new Brand { Id = SeedConstants.BrandEdifier,    Name = "EDIFIER",      Description = "Loa âm thanh chất lượng" },
                        new Brand { Id = SeedConstants.BrandHyperX,     Name = "HYPERX",       Description = "Gaming peripherals Kingston" },
                        new Brand { Id = SeedConstants.BrandSteelSeries, Name = "STEELSERIES", Description = "Thiết bị gaming đỉnh cao" },
                        new Brand { Id = SeedConstants.BrandLg,         Name = "LG",           Description = "Màn hình IPS & OLED hàng đầu" },
                        new Brand { Id = SeedConstants.BrandBenq,       Name = "BENQ",         Description = "Màn hình chuyên nghiệp" },
                    };

                    await context.Brands.AddRangeAsync(brands);
                    await context.SaveChangesAsync();

                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Brands] OFF");
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
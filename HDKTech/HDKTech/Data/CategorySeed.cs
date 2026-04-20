using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    public static class CategorySeed
    {
        public static async Task SeedAsync(HDKTechContext context)
        {
            if (await context.Categories.AnyAsync()) return;

            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Categories] ON");

                    // ── Tầng 1: Root categories ─────────────────────────────────
                    var roots = new[]
                    {
                        new Category { Id = SeedConstants.CatLaptop,        Name = "Laptop",                      Description = "Máy tính xách tay" },
                        new Category { Id = SeedConstants.CatLaptopGaming,  Name = "Laptop Gaming",               Description = "Laptop hiệu năng cao cho game" },
                        new Category { Id = SeedConstants.CatPcGvn,         Name = "PC GVN",                      Description = "PC Gaming đóng gói sẵn" },
                        new Category { Id = SeedConstants.CatMainCpuVga,    Name = "Main, CPU, VGA",              Description = "Linh kiện chủ lực" },
                        new Category { Id = SeedConstants.CatCaseNguonTan,  Name = "Case, Nguồn, Tản",            Description = "Vỏ máy và hệ thống làm mát" },
                        new Category { Id = SeedConstants.CatStorageRam,    Name = "Ổ cứng, RAM, Thẻ nhớ",       Description = "Lưu trữ và bộ nhớ" },
                        new Category { Id = SeedConstants.CatLoaMicWebcam,  Name = "Loa, Micro, Webcam",          Description = "Thiết bị âm thanh và hình ảnh" },
                        new Category { Id = SeedConstants.CatManHinh,       Name = "Màn hình",                    Description = "Monitor gaming và văn phòng" },
                        new Category { Id = SeedConstants.CatBanPhim,       Name = "Bàn phím",                    Description = "Bàn phím cơ và membrane" },
                        new Category { Id = SeedConstants.CatChuot,         Name = "Chuột + Lót chuột",           Description = "Chuột gaming và phụ kiện" },
                        new Category { Id = SeedConstants.CatTaiNghe,       Name = "Tai nghe",                    Description = "Tai nghe gaming" },
                        new Category { Id = SeedConstants.CatHandheld,      Name = "Handheld, Console",           Description = "Máy chơi game cầm tay" },
                    };
                    await context.Categories.AddRangeAsync(roots);
                    await context.SaveChangesAsync();

                    // ── Tầng 2 & 3: Sub-categories ──────────────────────────────
                    int nextId = 100;

                    // Gán lại nextId sau mỗi lần gọi để ID tăng tiến liên tục
                    nextId = await AddSubs(context, SeedConstants.CatLaptop, nextId, new[]
                    {
                        ("Thương hiệu", new[] { "ASUS", "DELL", "HP", "LENOVO", "Apple MacBook", "ACER" }),
                        ("Giá bán",     new[] { "Dưới 15 triệu", "15 - 20 triệu", "Trên 20 triệu" }),
                        ("CPU Intel",   new[] { "Core i5", "Core i7", "Core Ultra" }),
                    });

                    nextId = await AddSubs(context, SeedConstants.CatLaptopGaming, nextId, new[]
                    {
                        ("Thương hiệu Gaming", new[] { "ASUS ROG", "MSI Katana", "Acer Predator", "Lenovo Legion" }),
                        ("VGA rời",            new[] { "RTX 4050", "RTX 4060", "RTX 4070", "RTX 4080", "RTX 4090" }),
                    });

                    nextId = await AddSubs(context, SeedConstants.CatPcGvn, nextId, new[]
                    {
                        ("PC Theo giá",     new[] { "Dưới 20 triệu", "20-50 triệu", "Trên 50 triệu" }),
                        ("PC Theo cấu hình", new[] { "PC RTX 4060", "PC RTX 4070", "PC RTX 4080", "PC RTX 5090" }),
                    });

                    nextId = await AddSubs(context, SeedConstants.CatMainCpuVga, nextId, new[]
                    {
                        ("VGA RTX 50 Series", new[] { "RTX 5090", "RTX 5080", "RTX 5070 Ti" }),
                        ("CPU Intel Core",    new[] { "Core Ultra 9", "Core Ultra 7", "Core i9" }),
                        ("Bo mạch chủ",       new[] { "Z890", "Z790", "B760" }),
                    });

                    nextId = await AddSubs(context, SeedConstants.CatStorageRam, nextId, new[]
                    {
                        ("Dung lượng RAM", new[] { "8 GB", "16 GB", "32 GB", "64 GB" }),
                        ("Dung lượng SSD", new[] { "256 GB", "512 GB", "1 TB", "2 TB+" }),
                    });

                    nextId = await AddSubs(context, SeedConstants.CatManHinh, nextId, new[]
                    {
                        ("Độ phân giải", new[] { "Full HD 1080p", "2K 1440p", "4K UHD" }),
                        ("Tần số quét",  new[] { "144Hz", "165Hz", "240Hz", "360Hz" }),
                    });

                    nextId = await AddSubs(context, SeedConstants.CatBanPhim, nextId, new[]
                    {
                        ("Thương hiệu", new[] { "AKKO", "Corsair", "Keychron", "Razer" }),
                    });

                    nextId = await AddSubs(context, SeedConstants.CatChuot, nextId, new[]
                    {
                        ("Thương hiệu", new[] { "Logitech", "Razer", "Corsair", "SteelSeries" }),
                        ("Lót chuột",   new[] { "Lót nhỏ", "Lót vừa", "Lót lớn" }),
                    });

                    nextId = await AddSubs(context, SeedConstants.CatTaiNghe, nextId, new[]
                    {
                        ("Thương hiệu", new[] { "HyperX", "Corsair", "Razer", "SteelSeries" }),
                    });

                    nextId = await AddSubs(context, SeedConstants.CatHandheld, nextId, new[]
                    {
                        ("Handheld PC", new[] { "ASUS ROG Ally", "Legion Go" }),
                        ("Console",     new[] { "PlayStation 5", "Nintendo Switch" }),
                    });

                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Categories] OFF");
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        private static async Task<int> AddSubs(
            HDKTechContext ctx,
            int parentId,
            int nextId,
            (string groupName, string[] children)[] groups)
        {
            foreach (var (groupName, children) in groups)
            {
                var group = new Category { Id = nextId++, Name = groupName, ParentCategoryId = parentId };
                ctx.Categories.Add(group);
                await ctx.SaveChangesAsync();

                foreach (var child in children)
                    ctx.Categories.Add(new Category { Id = nextId++, Name = child, ParentCategoryId = group.Id });

                await ctx.SaveChangesAsync();
            }
            return nextId;
        }
    }
}
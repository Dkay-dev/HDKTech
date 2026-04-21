using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    public static class SystemLogSeed
    {
        public static async Task SeedAsync(HDKTechContext context)
        {
            if (await context.SystemLogs.AnyAsync()) return;

            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [SystemLogs] ON");

                    var now = DateTime.Now;
                    var logs = new List<SystemLog>();
                    int id = 1;

                    // Đủ loại action để dropdown filter có dữ liệu
                    var entries = new (string user, string action, string module, string desc, int daysAgo, int hoursAgo)[]
                    {
                        ("admin@hdktech.vn", "Login",  "System",  "Đăng nhập thành công từ IP 118.69.xx.xx",   0, 1),
                        ("admin@hdktech.vn", "Create", "Product", "Thêm sản phẩm ASUS ROG Strix G16 2024",     0, 2),
                        ("admin@hdktech.vn", "Update", "Order",   "Cập nhật đơn HDK20241201001 → Đã giao",     0, 3),
                        ("admin@hdktech.vn", "Create", "Banner",  "Thêm banner Siêu Sale 12.12",               1, 8),
                        ("admin@hdktech.vn", "Update", "Product", "Cập nhật giá RTX 5090: 95tr → 92.99tr",    1, 10),
                        ("admin@hdktech.vn", "Delete", "Product", "Xóa sản phẩm test #999",                   2, 5),
                        ("admin@hdktech.vn", "Create", "Promotion","Tạo chiến dịch Black Friday 2024",         2, 14),
                        ("admin@hdktech.vn", "Login",  "System",  "Đăng nhập từ IP 118.69.xx.xx",             3, 9),
                        ("admin@hdktech.vn", "Update", "Order",   "Cập nhật đơn HDK20241130001 → Đã giao",    3, 11),
                        ("admin@hdktech.vn", "Create", "Category","Thêm danh mục GPU RTX 50 Series",          5, 7),
                        ("admin@hdktech.vn", "Update", "Banner",  "Kích hoạt banner Ưu đãi Liên Chiểu",       6, 3),
                        ("admin@hdktech.vn", "Login",  "System",  "Đăng nhập lúc mở cửa",                    7, 8),
                        ("admin@hdktech.vn", "Delete", "Order",   "Xóa đơn hàng test HDK00000001",            8, 15),
                        ("admin@hdktech.vn", "Update", "Product", "Cập nhật mô tả MacBook Air M3",            9, 6),
                        ("admin@hdktech.vn", "Logout", "System",  "Đăng xuất",                               10, 17),
                    };

                    foreach (var (user, action, module, desc, daysAgo, hoursAgo) in entries)
                    {
                        logs.Add(new SystemLog
                        {
                            Id = id++,
                            Username = user,
                            Action = action,
                            LogLevel = module,
                            Description = desc,
                            CreatedAt = now.AddDays(-daysAgo).AddHours(-hoursAgo),
                            IpAddress = "118.69.72.15",
                            Status = "Success",
                            UserId = SeedConstants.AdminUserId
                        });
                    }

                    await context.SystemLogs.AddRangeAsync(logs);
                    await context.SaveChangesAsync();

                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [SystemLogs] OFF");
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
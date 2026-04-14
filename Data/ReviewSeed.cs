using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    public static class ReviewSeed
    {
        public static async Task SeedAsync(HDKTechContext context)
        {
            if (await context.Reviews.AnyAsync()) return;

            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Reviews] ON");

                    var rng = new Random(99);
                    var now = DateTime.Now;

                    var comments = new[]
                    {
                        "Sản phẩm chất lượng vượt trội, giao hàng nhanh, đóng gói cẩn thận. Mua tại HDKTech Đà Nẵng rất yên tâm!",
                        "Đúng như mô tả, hiệu năng tốt, máy chạy mát, không bị throttle khi tải nặng. Sẽ quay lại mua tiếp.",
                        "Nhân viên tư vấn nhiệt tình, giá cạnh tranh. Xuất VAT nhanh gọn. Highly recommended!",
                        "Hàng chính hãng 100%, có tem DSS và BH hãng. Shop uy tín số 1 Đà Nẵng.",
                        "Build chắc chắn, màn hình sắc nét, bàn phím gõ êm. Xứng đáng từng đồng tiền bỏ ra.",
                        "Giao nhanh trong 2 giờ nội thành Đà Nẵng. Máy cũ đổi lên đây ngon hơn nhiều lần.",
                        "Rất hài lòng! Cấu hình đúng như quảng cáo, nhiệt độ thấp, không có dead pixel.",
                        "Shop chăm sóc sau mua tốt, có vấn đề gọi là được hỗ trợ ngay. Cảm ơn HDKTech!",
                    };

                    // Tạo review cho các sản phẩm phổ biến
                    var targetProducts = new[] { 1, 2, 4, 11, 12, 13, 14, 29, 30, 37, 38, 61, 62 };
                    var users = new[] {
                        SeedConstants.User1Id,
                        SeedConstants.User2Id,
                        SeedConstants.User3Id,
                        SeedConstants.User4Id,
                        SeedConstants.User5Id
                    };

                    int reviewId = 1;
                    foreach (var productId in targetProducts)
                    {
                        int reviewCount = rng.Next(2, 5);
                        for (int i = 0; i < reviewCount; i++)
                        {
                            context.Reviews.Add(new Review
                            {
                                Id = reviewId++,
                                ProductId = productId,
                                UserId = users[rng.Next(users.Length)],
                                Content = comments[rng.Next(comments.Length)],
                                Rating = rng.Next(4, 6),
                                ReviewDate = now.AddDays(-rng.Next(1, 60))
                            });
                        }
                    }

                    await context.SaveChangesAsync();

                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Reviews] OFF");
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
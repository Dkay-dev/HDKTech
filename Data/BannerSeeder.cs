using HDKTech.Areas.Admin.Models;
using HDKTech.Models;

namespace HDKTech.Data
{
    public static class BannerSeeder
    {
        public static async Task SeedBannersAsync(HDKTechContext context)
        {
            // Check if banners already exist
            if (context.Banners.Any())
            {
                return;
            }

            var banners = new List<Banner>
            {
                new Banner
                {
                    Title = "Khuyến Mãi Mùa Hè - Giảm Đến 50%",
                    Description = "Cơ hội vàng để mua sắm những sản phẩm công nghệ hàng đầu với giá tốt nhất",
                    ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=1200&h=400&fit=crop",
                    LinkUrl = "/",
                    BannerType = "Main",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(30)
                },
                new Banner
                {
                    Title = "Laptop Gaming Terbaru - Giảm 30%",
                    Description = "Trải nghiệm chơi game mượt mà với công nghệ RTX mới nhất",
                    ImageUrl = "https://images.unsplash.com/photo-1588872657840-2df7e3f60482?w=1200&h=400&fit=crop",
                    LinkUrl = "/category/1",
                    BannerType = "Main",
                    DisplayOrder = 2,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(30)
                },
                new Banner
                {
                    Title = "Linh Kiện Máy Tính - Chất Lượng Cao",
                    Description = "Chúng tôi cung cấp các linh kiện máy tính chính hãng với bảo hành đầy đủ",
                    ImageUrl = "https://images.unsplash.com/photo-1587829191301-dc798b83add3?w=1200&h=400&fit=crop",
                    LinkUrl = "/category/2",
                    BannerType = "Main",
                    DisplayOrder = 3,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(30)
                },
                new Banner
                {
                    Title = "Ưu Đãi Đặc Biệt Cho Thành Viên",
                    Description = "Đăng ký hôm nay và nhận 100.000đ mã giảm giá",
                    ImageUrl = "https://images.unsplash.com/photo-1607082348824-0a96f2a4b9da?w=400&h=400&fit=crop",
                    LinkUrl = "/",
                    BannerType = "Side",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(60)
                },
                new Banner
                {
                    Title = "Hỗ Trợ 24/7 - Gọi Ngay",
                    Description = "Đội hỗ trợ khách hàng của chúng tôi sẵn sàng giúp bạn",
                    ImageUrl = "https://images.unsplash.com/photo-1552664730-d307ca884978?w=400&h=400&fit=crop",
                    LinkUrl = "/",
                    BannerType = "Side",
                    DisplayOrder = 2,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(60)
                },
                new Banner
                {
                    Title = "Giao Hàng Nhanh Toàn Quốc - Miễn Phí",
                    Description = "Mua hàng ngay hôm nay, giao hàng ngay hôm sau",
                    ImageUrl = "https://images.unsplash.com/photo-1610512387693-7ad7b8a991d2?w=1200&h=300&fit=crop",
                    LinkUrl = "/",
                    BannerType = "Bottom",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(90)
                },
                new Banner
                {
                    Title = "Chuyên Nghiệp Và Đáng Tin Cậy",
                    Description = "20 năm kinh nghiệm trong ngành công nghệ",
                    ImageUrl = "https://images.unsplash.com/photo-1522869635100-ce306e08cd53?w=1200&h=300&fit=crop",
                    LinkUrl = "/",
                    BannerType = "Bottom",
                    DisplayOrder = 2,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(90)
                }
            };

            await context.Banners.AddRangeAsync(banners);
            await context.SaveChangesAsync();
        }
    }
}



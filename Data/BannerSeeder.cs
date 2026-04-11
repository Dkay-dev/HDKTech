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
                    TenBanner = "Khuyến Mãi Mùa Hè - Giảm Đến 50%",
                    MoTa = "Cơ hội vàng để mua sắm những sản phẩm công nghệ hàng đầu với giá tốt nhất",
                    ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=1200&h=400&fit=crop",
                    LinkBanner = "/",
                    LoaiBanner = "Main",
                    ThuTuHienThi = 1,
                    IsActive = true,
                    NgayTao = DateTime.Now,
                    NgayBatDau = DateTime.Now,
                    NgayKetThuc = DateTime.Now.AddDays(30)
                },
                new Banner
                {
                    TenBanner = "Laptop Gaming Terbaru - Giảm 30%",
                    MoTa = "Trải nghiệm chơi game mượt mà với công nghệ RTX mới nhất",
                    ImageUrl = "https://images.unsplash.com/photo-1588872657840-2df7e3f60482?w=1200&h=400&fit=crop",
                    LinkBanner = "/category/1",
                    LoaiBanner = "Main",
                    ThuTuHienThi = 2,
                    IsActive = true,
                    NgayTao = DateTime.Now,
                    NgayBatDau = DateTime.Now,
                    NgayKetThuc = DateTime.Now.AddDays(30)
                },
                new Banner
                {
                    TenBanner = "Linh Kiện Máy Tính - Chất Lượng Cao",
                    MoTa = "Chúng tôi cung cấp các linh kiện máy tính chính hãng với bảo hành đầy đủ",
                    ImageUrl = "https://images.unsplash.com/photo-1587829191301-dc798b83add3?w=1200&h=400&fit=crop",
                    LinkBanner = "/category/2",
                    LoaiBanner = "Main",
                    ThuTuHienThi = 3,
                    IsActive = true,
                    NgayTao = DateTime.Now,
                    NgayBatDau = DateTime.Now,
                    NgayKetThuc = DateTime.Now.AddDays(30)
                },
                new Banner
                {
                    TenBanner = "Ưu Đãi Đặc Biệt Cho Thành Viên",
                    MoTa = "Đăng ký hôm nay và nhận 100.000đ mã giảm giá",
                    ImageUrl = "https://images.unsplash.com/photo-1607082348824-0a96f2a4b9da?w=400&h=400&fit=crop",
                    LinkBanner = "/",
                    LoaiBanner = "Side",
                    ThuTuHienThi = 1,
                    IsActive = true,
                    NgayTao = DateTime.Now,
                    NgayBatDau = DateTime.Now,
                    NgayKetThuc = DateTime.Now.AddDays(60)
                },
                new Banner
                {
                    TenBanner = "Hỗ Trợ 24/7 - Gọi Ngay",
                    MoTa = "Đội hỗ trợ khách hàng của chúng tôi sẵn sàng giúp bạn",
                    ImageUrl = "https://images.unsplash.com/photo-1552664730-d307ca884978?w=400&h=400&fit=crop",
                    LinkBanner = "/",
                    LoaiBanner = "Side",
                    ThuTuHienThi = 2,
                    IsActive = true,
                    NgayTao = DateTime.Now,
                    NgayBatDau = DateTime.Now,
                    NgayKetThuc = DateTime.Now.AddDays(60)
                },
                new Banner
                {
                    TenBanner = "Giao Hàng Nhanh Toàn Quốc - Miễn Phí",
                    MoTa = "Mua hàng ngay hôm nay, giao hàng ngay hôm sau",
                    ImageUrl = "https://images.unsplash.com/photo-1610512387693-7ad7b8a991d2?w=1200&h=300&fit=crop",
                    LinkBanner = "/",
                    LoaiBanner = "Bottom",
                    ThuTuHienThi = 1,
                    IsActive = true,
                    NgayTao = DateTime.Now,
                    NgayBatDau = DateTime.Now,
                    NgayKetThuc = DateTime.Now.AddDays(90)
                },
                new Banner
                {
                    TenBanner = "Chuyên Nghiệp Và Đáng Tin Cậy",
                    MoTa = "20 năm kinh nghiệm trong ngành công nghệ",
                    ImageUrl = "https://images.unsplash.com/photo-1522869635100-ce306e08cd53?w=1200&h=300&fit=crop",
                    LinkBanner = "/",
                    LoaiBanner = "Bottom",
                    ThuTuHienThi = 2,
                    IsActive = true,
                    NgayTao = DateTime.Now,
                    NgayBatDau = DateTime.Now,
                    NgayKetThuc = DateTime.Now.AddDays(90)
                }
            };

            await context.Banners.AddRangeAsync(banners);
            await context.SaveChangesAsync();
        }
    }
}

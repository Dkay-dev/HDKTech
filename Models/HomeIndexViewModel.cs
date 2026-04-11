using System.Collections.Generic;

namespace HDKTech.Models
{
    public class HomeIndexViewModel
    {
        public List<SanPham> FlashSaleProducts { get; set; } = new();
        public List<SanPham> TopSellerProducts { get; set; } = new();
        public List<SanPham> NewProducts { get; set; } = new();
        public List<SanPham> AllProducts { get; set; } = new();
        public List<DanhMuc> Categories { get; set; } = new();

        // 🆕 Banners
        public List<Banner> MainBanners { get; set; } = new();
        public List<Banner> SideBanners { get; set; } = new();
        public List<Banner> BottomBanners { get; set; } = new();
    }
}

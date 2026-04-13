using System.Collections.Generic;
using HDKTech.Areas.Admin.Models;

namespace HDKTech.Models
{
    public class HomeIndexViewModel
    {
        public List<Product> FlashSaleProducts { get; set; } = new();
        public List<Product> TopSellerProducts { get; set; } = new();
        public List<Product> NewProducts { get; set; } = new();
        public List<Product> AllProducts { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        // 🆕 Banners
        public List<Banner> MainBanners { get; set; } = new();
        public List<Banner> SideBanners { get; set; } = new();
        public List<Banner> BottomBanners { get; set; } = new();
    }
}

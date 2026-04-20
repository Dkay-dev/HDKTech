using HDKTech.Areas.Admin.Models;
using HDKTech.Models;

namespace HDKTech.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<Product>  FlashSaleProducts { get; set; } = new();
        public List<Product>  TopSellerProducts { get; set; } = new();
        public List<Product>  NewProducts       { get; set; } = new();
        public List<Product>  AllProducts       { get; set; } = new();
        public List<Category> Categories        { get; set; } = new();
        public DateTime?      FlashSaleEndTime  { get; set; }

        public List<Banner> MainBanners   { get; set; } = new();
        public List<Banner> SideBanners   { get; set; } = new();
        public List<Banner> BottomBanners { get; set; } = new();
    }
}

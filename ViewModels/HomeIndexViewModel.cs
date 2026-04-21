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
        public DateTime?      FlashSaleEndTime   { get; set; }
        public DateTime?      FlashSaleStartTime { get; set; }
        public Dictionary<int, DateTime> FlashSaleEndTimeByProduct { get; set; } = new();

        public List<Banner> MainBanners   { get; set; } = new();
        public List<Banner> SideBanners   { get; set; } = new();
        public List<Banner> BottomBanners { get; set; } = new();

        /// <summary>
        /// Side banner được nhóm theo CategoryId.
        /// Key = CategoryId, Value = tối đa 2 banner (đã sort theo DisplayOrder).
        /// Dùng trong Home/Index để hiển thị đúng banner bên cạnh mỗi danh mục.
        /// </summary>
        public Dictionary<int, List<Banner>> SideBannersByCategory { get; set; } = new();

        /// <summary>Helper: lấy tối đa 2 side banner cho một danh mục cụ thể.</summary>
        public List<Banner> GetSideBannersForCategory(int categoryId)
            => SideBannersByCategory.TryGetValue(categoryId, out var list) ? list : new();
    }
}

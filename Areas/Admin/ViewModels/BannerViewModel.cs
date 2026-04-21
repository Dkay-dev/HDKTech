using HDKTech.Areas.Admin.Models;

namespace HDKTech.Areas.Admin.ViewModels
{
    /// <summary>
    /// ViewModel cho trang Banner Index - hiển thị danh sách + thống kê
    /// </summary>
    public class BannerIndexViewModel
    {
        public List<Banner> Banners { get; set; } = new();
        public int TotalBanners { get; set; }
        public int ActiveBanners { get; set; }
        public int InactiveBanners { get; set; }
        public int MainBanners { get; set; }
        public int SideBanners { get; set; }
    }
}

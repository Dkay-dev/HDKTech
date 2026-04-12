using HDKTech.Areas.Admin.ViewModels;

namespace HDKTech.Areas.Admin.Services
{
    /// <summary>
    /// Interface cho Dashboard Service - tách biệt logic tính toán ra khỏi Controller
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Lấy toàn bộ dữ liệu cho Dashboard (async)
        /// </summary>
        Task<DashboardViewModel> GetDashboardDataAsync();
    }
}

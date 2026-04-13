using HDKTech.Areas.Admin.ViewModels;

namespace HDKTech.Areas.Admin.Services
{
    /// <summary>
    /// Giai đoạn 2 — Observability: Dashboard Service với Caching &amp; Banner Analytics.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Lấy toàn bộ dữ liệu Dashboard.
        /// Kết quả được cache 5 phút — chỉ query DB khi cache hết hạn.
        /// </summary>
        Task<DashboardViewModel> GetDashboardDataAsync();

        /// <summary>
        /// Xóa cache thủ công — gọi sau khi có thay đổi lớn (vd: tạo đơn, hủy đơn).
        /// </summary>
        Task InvalidateCacheAsync();
    }
}

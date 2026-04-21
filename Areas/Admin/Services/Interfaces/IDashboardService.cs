using HDKTech.Areas.Admin.ViewModels;

namespace HDKTech.Areas.Admin.Services.Interfaces
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
        /// Lấy dữ liệu Dashboard đã lọc theo Role người dùng.
        /// Staff chỉ nhận dữ liệu Tồn kho + Đơn cần xử lý.
        /// </summary>
        Task<DashboardViewModel> GetDashboardDataAsync(string viewerRole);

        /// <summary>
        /// Xóa cache thủ công — gọi sau khi có thay đổi lớn (vd: tạo đơn, hủy đơn).
        /// </summary>
        Task InvalidateCacheAsync();
    }
}

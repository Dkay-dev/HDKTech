using HDKTech.Areas.Admin.Models;
using HDKTech.Models;
using HDKTech.Services;   // LowStockProductItem

namespace HDKTech.Areas.Admin.ViewModels
{
    /// <summary>
    /// ViewModel đầy đủ cho Dashboard Admin - truyền toàn bộ dữ liệu từ Service lên View
    /// </summary>
    public class DashboardViewModel
    {
        // ── Stats Cards ────────────────────────────────────────────────────
        /// <summary>Tổng doanh thu từ đơn hàng Status == 3 (Đã giao)</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>Tổng số đơn hàng</summary>
        public int TotalOrders { get; set; }

        /// <summary>Đơn hàng đang chờ xử lý (Status 0 hoặc 1)</summary>
        public int PendingOrders { get; set; }

        /// <summary>Số sản phẩm tồn kho thấp (< 10)</summary>
        public int LowStockCount { get; set; }

        /// <summary>
        /// Giai đoạn 1 — Danh sách chi tiết sản phẩm tồn kho thấp.
        /// Dùng để hiển thị cảnh báo đỏ trên Dashboard (badge + bảng).
        /// </summary>
        public List<LowStockProductItem> LowStockProducts { get; set; } = new();

        /// <summary>Số khách hàng mới trong 30 ngày</summary>
        public int NewCustomers { get; set; }

        /// <summary>Số khuyến mãi đang hoạt động</summary>
        public int ActivePromotions { get; set; }

        /// <summary>Tổng số Banner đang có trong hệ thống</summary>
        public int TotalBanners { get; set; }

        /// <summary>Số Banner đang active</summary>
        public int ActiveBanners { get; set; }

        // ── Chart Data (7 ngày gần nhất) ───────────────────────────────────
        /// <summary>Dữ liệu doanh thu 7 ngày để vẽ biểu đồ Chart.js</summary>
        public List<DailyRevenueData> DailyRevenue { get; set; } = new();

        // ── Recent Data Tables ─────────────────────────────────────────────
        /// <summary>5 đơn hàng gần nhất</summary>
        public List<Order> RecentOrders { get; set; } = new();

        /// <summary>5 hành động mới nhất từ Audit Log (SystemLog)</summary>
        public List<RecentAuditLogItem> RecentAuditLogs { get; set; } = new();

        // ── Giai đoạn 2: Observability ─────────────────────────────────────

        /// <summary>Số đơn hàng đặt trong ngày hôm nay</summary>
        public int TodayOrderCount { get; set; }

        /// <summary>Doanh thu hôm nay (chỉ tính đơn Status == 3)</summary>
        public decimal TodayRevenue { get; set; }

        /// <summary>Top 3 Banner hiệu quả nhất (theo Click)</summary>
        public List<BannerRoiItem> TopBanners { get; set; } = new();

        /// <summary>Tổng số lần click Banner hôm nay</summary>
        public int TotalClicksToday { get; set; }

        /// <summary>Giá trị đơn hàng trung bình (tính từ đơn Đã giao)</summary>
        public decimal AverageOrderValue { get; set; }

        /// <summary>Timestamp cache được tạo — hiển thị "cập nhật lần cuối"</summary>
        public DateTime CachedAt { get; set; } = DateTime.Now;

        // ── Sprint 3: Role-based Experience ───────────────────────────────────
        /// <summary>
        /// Nếu false (WarehouseStaff), ẩn toàn bộ widget doanh thu &amp; biểu đồ doanh thu.
        /// Chỉ hiện dữ liệu Tồn kho và Đơn cần xử lý.
        /// </summary>
        public bool ShowRevenueData { get; set; } = true;

        /// <summary>Role của người xem — dùng để điều chỉnh UI trong View.</summary>
        public string ViewerRole { get; set; } = string.Empty;
    }

    /// <summary>Dữ liệu doanh thu từng ngày cho biểu đồ</summary>
    public class DailyRevenueData
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    /// <summary>Item rút gọn của Audit Log cho Dashboard</summary>
    public class RecentAuditLogItem
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Success";
    }

    // ── Giai đoạn 2: Banner ROI ─────────────────────────────────────────────

    /// <summary>
    /// Chỉ số hiệu quả của một Banner — dùng cho bảng Top 3 trên Dashboard.
    /// Revenue được ước tính: ClicksLast7Days × tỷ lệ chuyển đổi 5% × AOV.
    /// </summary>
    public class BannerRoiItem
    {
        public int    BannerId        { get; set; }
        public string BannerTitle     { get; set; } = string.Empty;
        public string BannerType      { get; set; } = "Main";
        public string BannerUrl       { get; set; } = "#";

        public int  TotalClicks      { get; set; }
        public int  ClicksLast7Days  { get; set; }
        public int  ClicksToday      { get; set; }
        public int  UniqueReach      { get; set; }   // Unique IPs

        /// <summary>Ước tính DT = ClicksLast7D × 5% conv × AOV (cần session tracking để chính xác)</summary>
        public decimal EstimatedRevenue { get; set; }

        /// <summary>Trung bình click/ngày trong 7 ngày gần nhất</summary>
        public decimal AvgClicksPerDay =>
            ClicksLast7Days > 0 ? Math.Round((decimal)ClicksLast7Days / 7, 1) : 0;

        /// <summary>Xếp hạng hiệu quả</summary>
        public string EfficiencyBadge => TotalClicks switch
        {
            > 500 => "danger",    // 🔥 Viral
            > 100 => "warning",   // ⚡ Hot
            > 20  => "success",   // ✅ Active
            _     => "secondary"  // 📊 Low
        };

        public string EfficiencyLabel => TotalClicks switch
        {
            > 500 => "Viral",
            > 100 => "Hot",
            > 20  => "Active",
            _     => "Low"
        };
    }
}

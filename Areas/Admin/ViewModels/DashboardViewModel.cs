using HDKTech.Areas.Admin.Models;
using HDKTech.Models;

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
}

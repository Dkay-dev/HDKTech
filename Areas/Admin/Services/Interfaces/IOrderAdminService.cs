using HDKTech.Models;

namespace HDKTech.Areas.Admin.Services.Interfaces
{
    public class OrderListResult
    {
        public List<Order> Orders    { get; set; } = new();
        public int TotalCount        { get; set; }
        public int PendingCount      { get; set; }
        public int ProcessingCount   { get; set; }
        public int ShippingCount     { get; set; }
        public int DeliveredCount    { get; set; }
        public int CancelledCount    { get; set; }
        public decimal TodayRevenue  { get; set; }
        public int TodayOrderCount   { get; set; }
    }

    public interface IOrderAdminService
    {
        Task<OrderListResult> GetOrdersPagedAsync(
            int page, int pageSize, string searchTerm, int statusFilter, string sortBy);

        Task<Order?> GetOrderDetailsAsync(int id);

        Task<(bool Success, string Message)> UpdateStatusAsync(
            int orderId, int newStatus, string username, string? userId);

        Task<(bool Success, string Message)> CancelOrderAsync(
            int orderId, string username, string? userId);

        Task<List<Order>> GetOrdersForExportAsync(string searchTerm, int statusFilter);
    }
}

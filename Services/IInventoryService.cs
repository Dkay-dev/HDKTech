using HDKTech.Models;

namespace HDKTech.Services
{
    /// <summary>
    /// Giai đoạn 1 — Inventory Sync
    /// Contract cho mọi thao tác tồn kho: trừ kho, hoàn kho, cảnh báo.
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>
        /// Trừ kho khi đặt hàng thành công.
        /// Chỉ thay đổi entity trong DbContext — KHÔNG gọi SaveChanges.
        /// Được gọi bên trong Database Transaction của CreateOrderAsync.
        /// </summary>
        Task<(bool Success, string Message)> ReserveStockAsync(List<CartItem> items);

        /// <summary>
        /// Hoàn kho khi đơn hàng bị hủy.
        /// Tự gọi SaveChanges + ghi Audit Log sau khi cập nhật.
        /// </summary>
        Task<bool> ReleaseStockAsync(
            List<OrderItem> items,
            string username = "System",
            string userId = null);

        /// <summary>
        /// Kiểm tra đủ hàng trước khi cho phép đặt hàng (AsNoTracking).
        /// Trả về false ngay khi gặp sản phẩm đầu tiên không đủ.
        /// </summary>
        Task<bool> CheckStockAvailabilityAsync(List<CartItem> items);

        /// <summary>
        /// Lấy danh sách sản phẩm tồn kho thấp để hiển thị cảnh báo Dashboard.
        /// </summary>
        Task<List<LowStockProductItem>> GetLowStockProductsAsync(int threshold = 5);
    }

    /// <summary>DTO sản phẩm tồn kho thấp — dùng cho Dashboard và cảnh báo.</summary>
    public class LowStockProductItem
    {
        public int    ProductId    { get; set; }
        public string ProductName  { get; set; } = string.Empty;
        public int    CurrentStock { get; set; }
        public int    Threshold    { get; set; }

        /// <summary>Nguy hiểm: ≤ 2 sản phẩm → hiển thị badge đỏ đậm.</summary>
        public bool IsCritical => CurrentStock <= 2;
    }
}

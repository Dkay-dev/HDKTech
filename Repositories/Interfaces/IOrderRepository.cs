using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        /// <summary>
        /// Tạo đơn hàng (COD / trực tiếp).
        /// Bọc trong DB transaction, re-fetch giá từ DB.
        /// </summary>
        Task<Order> CreateOrderAsync(string userId, string RecipientName, string soDienThoai,
                                     string ShippingAddress, List<CartItem> items,
                                     decimal ShippingFee = 0,
                                     string paymentMethod = null,
                                     string paymentStatus = null);

        /// <summary>
        /// Tạo đơn hàng từ PendingCheckout (sau khi payment gateway callback).
        /// Đảm bảo idempotent — PendingCheckout.Status = Paid sau khi tạo thành công.
        /// </summary>
        Task<(bool Success, string? Error, Order? Order)> CreateFromPendingCheckoutAsync(
            PendingCheckout pending);

        /// <summary>Lấy đơn hàng theo mã đơn hàng.</summary>
        Task<Order> GetOrderByMaDonHangAsync(string OrderCode);

        /// <summary>Lấy tất cả đơn hàng của một user.</summary>
        Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);

        /// <summary>Cập nhật trạng thái đơn hàng.</summary>
        Task<bool> UpdateOrderStatusAsync(int maOrder, int trangThaiMoi);

        /// <summary>Xóa đơn hàng.</summary>
        Task<bool> DeleteOrderAsync(int maOrder);
    }
}



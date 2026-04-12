using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        /// <summary>
        /// Tạo đơn hàng mới và lưu vào database
        /// </summary>
        Task<Order> CreateOrderAsync(string userId, string RecipientName, string soDienThoai, 
                                       string ShippingAddress, List<CartItem> items, decimal ShippingFee = 0);

        /// <summary>
        /// Lấy đơn hàng theo mã đơn hàng
        /// </summary>
        Task<Order> GetOrderByMaDonHangAsync(string OrderCode);

        /// <summary>
        /// Lấy tất cả đơn hàng của một user
        /// </summary>
        Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);

        /// <summary>
        /// Cập nhật trạng thái đơn hàng
        /// </summary>
        Task<bool> UpdateOrderStatusAsync(int maOrder, int trangThaiMoi);

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        Task<bool> DeleteOrderAsync(int maOrder);
    }
}



using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    /// <summary>
    /// Interface quản lý giỏ hàng - Linh hoạt với bất kỳ storage nào (Session, Database, Redis)
    /// </summary>
    public interface ICartRepository
    {
        /// <summary>
        /// Lấy giỏ hàng của user
        /// </summary>
        Task<Cart> GetCartAsync(string userId);

        /// <summary>
        /// Thêm item vào giỏ
        /// </summary>
        Task AddItemAsync(string userId, CartItem item);

        /// <summary>
        /// Xóa item khỏi giỏ
        /// </summary>
        Task RemoveItemAsync(string userId, int productId);

        /// <summary>
        /// Cập nhật số lượng item
        /// </summary>
        Task UpdateQuantityAsync(string userId, int productId, int quantity);

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        Task ClearCartAsync(string userId);

        /// <summary>
        /// Lưu giỏ hàng (nếu cần lưu vào database)
        /// </summary>
        Task SaveCartAsync(string userId, Cart cart);
    }
}



using HDKTech.Models;
using System.Text.Json;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Services
{
    /// <summary>
    /// Service xử lý giỏ hàng qua Session
    /// Linh hoạt: có thể extend để dùng Redis, Database,...
    /// </summary>
    public interface ICartService
    {
        Task<Cart> GetCartAsync();
        Task AddItemAsync(CartItem item);
        Task RemoveItemAsync(int productId);
        Task UpdateQuantityAsync(int productId, int quantity);
        Task ClearCartAsync();
    }

    public class SessionCartService : ICartService
    {
        private const string CART_SESSION_KEY = "cart_items";
        private readonly ISession _session;

        public SessionCartService(IHttpContextAccessor httpContextAccessor)
        {
            _session = httpContextAccessor.HttpContext?.Session 
                ?? throw new ArgumentNullException(nameof(httpContextAccessor), "Session không khả dụng");
        }

        /// <summary>
        /// Lấy giỏ hàng từ Session
        /// </summary>
        public async Task<Cart> GetCartAsync()
        {
            var cart = new Cart();

            if (_session.TryGetValue(CART_SESSION_KEY, out byte[]? cartData))
            {
                try
                {
                    var json = System.Text.Encoding.UTF8.GetString(cartData);
                    var items = JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
                    cart.Items = items;
                }
                catch
                {
                    // Nếu lỗi parse, trả về giỏ trống
                    cart.Items = new List<CartItem>();
                }
            }

            return await Task.FromResult(cart);
        }

        /// <summary>
        /// Thêm item vào giỏ
        /// </summary>
        public async Task AddItemAsync(CartItem item)
        {
            var cart = await GetCartAsync();
            cart.AddItem(item);
            await SaveCartAsync(cart);
        }

        /// <summary>
        /// Xóa item khỏi giỏ
        /// </summary>
        public async Task RemoveItemAsync(int productId)
        {
            var cart = await GetCartAsync();
            cart.RemoveItem(productId);
            await SaveCartAsync(cart);
        }

        /// <summary>
        /// Cập nhật số lượng item
        /// </summary>
        public async Task UpdateQuantityAsync(int productId, int quantity)
        {
            var cart = await GetCartAsync();
            cart.UpdateQuantity(productId, quantity);
            await SaveCartAsync(cart);
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        public async Task ClearCartAsync()
        {
            _session.Remove(CART_SESSION_KEY);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Lưu giỏ hàng vào Session
        /// </summary>
        private async Task SaveCartAsync(Cart cart)
        {
            var json = JsonSerializer.Serialize(cart.Items);
            _session.SetString(CART_SESSION_KEY, json);
            await Task.CompletedTask;
        }
    }
}


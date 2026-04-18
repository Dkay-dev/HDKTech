using HDKTech.Models;
using System.Text.Json;

namespace HDKTech.Services
{
    /// <summary>
    /// Cart qua Session. Key của 1 item = (ProductId, ProductVariantId).
    /// </summary>
    public interface ICartService
    {
        Task<Cart> GetCartAsync();
        Task AddItemAsync(CartItem item);
        Task RemoveItemAsync(int productId, int productVariantId);
        Task UpdateQuantityAsync(int productId, int productVariantId, int quantity);
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

        public Task<Cart> GetCartAsync()
        {
            var cart = new Cart();

            if (_session.TryGetValue(CART_SESSION_KEY, out byte[]? cartData) && cartData != null)
            {
                try
                {
                    var json = System.Text.Encoding.UTF8.GetString(cartData);
                    cart.Items = JsonSerializer.Deserialize<List<CartItem>>(json) ?? new();
                }
                catch
                {
                    cart.Items = new();
                }
            }

            return Task.FromResult(cart);
        }

        public async Task AddItemAsync(CartItem item)
        {
            var cart = await GetCartAsync();
            cart.AddItem(item);
            SaveCart(cart);
        }

        public async Task RemoveItemAsync(int productId, int productVariantId)
        {
            var cart = await GetCartAsync();
            cart.RemoveItem(productId, productVariantId);
            SaveCart(cart);
        }

        public async Task UpdateQuantityAsync(int productId, int productVariantId, int quantity)
        {
            var cart = await GetCartAsync();
            cart.UpdateQuantity(productId, productVariantId, quantity);
            SaveCart(cart);
        }

        public Task ClearCartAsync()
        {
            _session.Remove(CART_SESSION_KEY);
            return Task.CompletedTask;
        }

        private void SaveCart(Cart cart)
        {
            var json = JsonSerializer.Serialize(cart.Items);
            _session.SetString(CART_SESSION_KEY, json);
        }
    }
}

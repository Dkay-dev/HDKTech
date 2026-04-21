namespace HDKTech.Services
{
    public interface ICartService
    {
        Task<Models.Cart> GetCartAsync();
        Task AddItemAsync(Models.CartItem item);
        Task RemoveItemAsync(int productId, int productVariantId);
        Task UpdateQuantityAsync(int productId, int productVariantId, int quantity);
        Task ClearCartAsync();
    }
}

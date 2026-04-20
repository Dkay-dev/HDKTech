// Services/IProductService.cs

// Services/IProductService.cs
using HDKTech.Models;

namespace HDKTech.Services.Interfaces
{
    public interface IProductService
    {
        /// <summary>
        /// Filter sản phẩm + tính toán filter options động.
        /// Đây là entry point chính cho trang Filter và Category.
        /// </summary>
        Task<ProductFilterResult> FilterAsync(ProductFilterModel filter);

        Task<Product?> GetDetailAsync(int id);
        Task<List<Product>> GetRelatedAsync(int productId, int categoryId, int limit = 8);
        Task<List<Product>> GetFlashSaleAsync(int limit = 5);
        Task<DateTime?> GetFlashSaleEndTimeAsync();
    }
}
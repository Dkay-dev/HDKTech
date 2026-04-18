using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    /// <summary>
    /// Alias interface cũ (namespace Repositories.Interfaces). Được giữ để
    /// tương thích với các controller / service import kiểu này.
    /// API hoàn toàn đồng bộ với HDKTech.Areas.Admin.Repositories.IAdminProductRepository.
    /// </summary>
    public interface IAdminProductRepository
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetProductsByBrandAsync(int brandId);
        Task<(IEnumerable<Product> products, int totalCount)> GetProductsPagedAsync(int pageNumber, int pageSize);

        Task<Product> CreateProductAsync(Product product);

        Task<bool> UpdateProductAsync(Product product);
        Task<bool> UpdateVariantStockAsync(int productVariantId, int quantity);
        Task<bool> UpdateVariantPriceAsync(int productVariantId, decimal price);

        Task<bool> DeleteProductAsync(int id);
        Task<bool> DeleteProductsAsync(IEnumerable<int> ids);

        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<Product>> FilterProductsAsync(ProductFilterCriteria criteria);

        Task<bool> ProductExistsAsync(int id);
        Task<bool> CheckSkuExistsAsync(string sku, int? excludeVariantId = null);
    }

    public class ProductFilterCriteria
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStock { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; } = "Name";
        public bool SortDescending { get; set; } = false;
    }
}

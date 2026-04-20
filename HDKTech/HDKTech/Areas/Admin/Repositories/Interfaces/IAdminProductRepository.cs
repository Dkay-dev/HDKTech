using HDKTech.Models;

namespace HDKTech.Areas.Admin.Repositories
{
    /// <summary>
    /// Interface for Product repository operations (Admin).
    /// Price/stock giờ thao tác qua ProductVariant + Inventory.
    /// </summary>
    public interface IAdminProductRepository
    {
        // Read
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetProductsByBrandAsync(int brandId);
        Task<(IEnumerable<Product> products, int totalCount)> GetProductsPagedAsync(int pageNumber, int pageSize);

        // Create
        Task<Product> CreateProductAsync(Product product);

        // Update
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> UpdateVariantStockAsync(int productVariantId, int quantity);
        Task<bool> UpdateVariantPriceAsync(int productVariantId, decimal price);

        // Delete (Module C: soft delete — deletedBy optional for audit)
        Task<(bool success, string? error, IList<string> imageUrls)> DeleteProductAsync(int id, string? deletedBy = null);
        Task<(int deleted, int skipped, IList<string> imageUrls)> DeleteProductsAsync(IEnumerable<int> ids, string? deletedBy = null);

        // Search & Filter
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<Product>> FilterProductsAsync(ProductFilterCriteria criteria);

        // Exists
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

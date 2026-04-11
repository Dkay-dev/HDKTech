using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    /// <summary>
    /// Interface for Product repository operations
    /// Handles all CRUD operations for SanPham (Product) model
    /// </summary>
    public interface IAdminProductRepository
    {
        // Read operations
        Task<IEnumerable<SanPham>> GetAllProductsAsync();
        Task<SanPham> GetProductByIdAsync(int id);
        Task<IEnumerable<SanPham>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<SanPham>> GetProductsByBrandAsync(int brandId);
        Task<(IEnumerable<SanPham> products, int totalCount)> GetProductsPagedAsync(int pageNumber, int pageSize);
        
        // Create operations
        Task<SanPham> CreateProductAsync(SanPham product);
        
        // Update operations
        Task<bool> UpdateProductAsync(SanPham product);
        Task<bool> UpdateProductStockAsync(int productId, int quantity);
        Task<bool> UpdateProductPriceAsync(int productId, decimal price);
        
        // Delete operations
        Task<bool> DeleteProductAsync(int id);
        Task<bool> DeleteProductsAsync(IEnumerable<int> ids);
        
        // Search and filter
        Task<IEnumerable<SanPham>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<SanPham>> FilterProductsAsync(ProductFilterCriteria criteria);
        
        // Check existence
        Task<bool> ProductExistsAsync(int id);
        Task<bool> CheckSkuExistsAsync(string sku, int? excludeProductId = null);
    }

    /// <summary>
    /// Filter criteria for product search and filtering
    /// </summary>
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

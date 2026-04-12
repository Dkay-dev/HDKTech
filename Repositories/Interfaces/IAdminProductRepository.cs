using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    /// <summary>
    /// Interface for Product repository operations
    /// Handles all CRUD operations for Product (Product) model
    /// </summary>
    public interface IAdminProductRepository
    {
        // Read operations
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetProductsByBrandAsync(int brandId);
        Task<(IEnumerable<Product> products, int totalCount)> GetProductsPagedAsync(int pageNumber, int pageSize);
        
        // Create operations
        Task<Product> CreateProductAsync(Product product);
        
        // Update operations
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> UpdateProductStockAsync(int productId, int quantity);
        Task<bool> UpdateProductPriceAsync(int productId, decimal price);
        
        // Delete operations
        Task<bool> DeleteProductAsync(int id);
        Task<bool> DeleteProductsAsync(IEnumerable<int> ids);
        
        // Search and filter
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<Product>> FilterProductsAsync(ProductFilterCriteria criteria);
        
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



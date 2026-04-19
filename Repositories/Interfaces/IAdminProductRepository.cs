using HDKTech.Areas.Admin.Repositories; // ProductFilterCriteria
using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    /// <summary>
    /// [DEPRECATED — Module C cleanup]
    /// Stub giữ namespace để không phá build. Toàn bộ logic đã chuyển về
    /// HDKTech.Areas.Admin.Repositories.IAdminProductRepository (tuple-based Delete).
    /// Các file còn inject interface này (BrandController, CategoryController, v.v.)
    /// dùng HDKTech.Repositories.Interfaces cho IBrand/ICategory — không dùng
    /// IAdminProductRepository từ namespace này nữa.
    /// DI registration cho interface này đã bị xoá khỏi Program.cs.
    /// </summary>
    [Obsolete("Use HDKTech.Areas.Admin.Repositories.IAdminProductRepository")]
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

        // Tuple-based Delete — đồng bộ với Areas.Admin version (Module C: soft delete)
        Task<(bool success, string? error, IList<string> imageUrls)> DeleteProductAsync(int id, string? deletedBy = null);
        Task<(int deleted, int skipped, IList<string> imageUrls)> DeleteProductsAsync(IEnumerable<int> ids, string? deletedBy = null);

        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<Product>> FilterProductsAsync(ProductFilterCriteria criteria);

        Task<bool> ProductExistsAsync(int id);
        Task<bool> CheckSkuExistsAsync(string sku, int? excludeVariantId = null);
    }
}

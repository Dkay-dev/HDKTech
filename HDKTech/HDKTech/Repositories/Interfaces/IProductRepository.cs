using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    public interface IProductRepository
    {
        // Flash Sale
        Task<List<Product>> GetFlashSaleProductsAsync(int limit = 5);

        // Module D: Paginated listing — thay thế GetAllWithImagesAsync() không giới hạn
        // pageSize mặc định 12, tối đa 48 (enforced ở caller)
        Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, ProductFilterModel? filter = null);

        // Listing
        Task<List<Product>> GetAllWithImagesAsync();

        // Detail page — bao gồm đầy đủ Relations
        Task<Product?> GetProductWithDetailsAsync(int id);

        // FIX: Tham số đúng thứ tự: currentProductId trước, categoryId sau
        Task<List<Product>> GetRelatedProductsAsync(int currentProductId, int categoryId, int limit);

        // FIX 2: FilterProductsAsync nhận đầy đủ tham số sidebar
        Task<List<Product>> FilterProductsAsync(ProductFilterModel filter);

        // Helpers cho dropdown filter
        Task<List<string>> GetUniqueBrandsByCategory(int categoryId);
        Task<List<string>> GetUniqueCpuLines();

        // Top / New sections
        Task<List<Product>> GetTopSellerProductsAsync(int limit = 8);
        Task<List<Product>> GetNewProductsAsync(int limit = 6);
    }
}
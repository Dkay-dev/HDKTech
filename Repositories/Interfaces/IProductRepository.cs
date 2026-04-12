using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllWithImagesAsync();
        Task<Product?> GetProductWithDetailsAsync(int id);
        Task<List<Product>> GetRelatedProductsAsync(int categoryId, int currentProductId, int limit);
        Task<List<Product>> FilterProductsAsync(ProductFilterModel filter);
        Task<List<string>> GetUniqueBrandsByCategory(int categoryId);
        Task<List<string>> GetUniqueCpuLines();
    }
}



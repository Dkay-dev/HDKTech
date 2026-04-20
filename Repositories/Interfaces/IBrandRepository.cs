using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    public interface IBrandRepository
    {
        Task<List<Brand>> GetAllAsync();
        Task<List<Brand>> GetAllWithProductCountAsync();
        Task<Brand?> GetByIdAsync(int id);
        Task<Brand?> GetByIdWithProductsAsync(int id);
        Task<Brand?> GetByNameWithProductsAsync(string name);
        Task<bool> AddAsync(Brand brand);
        Task<bool> UpdateAsync(Brand brand);
        Task<bool> DeleteAsync(int id);
        Task<bool> HasProductsAsync(int brandId);
        Task<int> CountAsync();
        Task<int> CountEmptyAsync();
    }
}

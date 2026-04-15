using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    public interface IReviewRepository
    {
        Task<List<Review>> GetByProductIdAsync(int productId);
        Task<Review?> GetByIdAsync(int id);
        Task AddAsync(Review review);
        Task UpdateAsync(Review review);
        Task DeleteAsync(int id);
        Task<bool> HasUserPurchasedProductAsync(string userId, int productId);
        Task<bool> HasUserReviewedProductAsync(string userId, int productId);
        Task<Review?> GetUserReviewForProductAsync(string userId, int productId);
        Task<double> GetAverageRatingAsync(int productId);
        Task<bool> SaveAsync();
    }
}

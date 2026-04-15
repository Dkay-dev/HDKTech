using HDKTech.Models;

namespace HDKTech.Services
{
    public interface IReviewService
    {
        Task<(bool Success, string Message, Review? Review)> AddReviewAsync(int productId, string userId, int rating, string content);
        Task<(bool Success, string Message)> UpdateReviewAsync(int reviewId, string userId, int rating, string content);
        Task<(bool Success, string Message)> DeleteReviewAsync(int reviewId, string userId);
        Task<List<Review>> GetProductReviewsAsync(int productId);
        Task<bool> CanUserReviewAsync(string userId, int productId);
        Task<Review?> GetUserReviewAsync(string userId, int productId);
        Task<double> GetAverageRatingAsync(int productId);
    }
}

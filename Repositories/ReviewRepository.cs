using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        public ReviewRepository(HDKTechContext context) : base(context) { }

        public async Task<List<Review>> GetByProductIdAsync(int productId)
        {
            return await _dbSet
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();
        }

        public async Task<bool> HasUserPurchasedProductAsync(string userId, int productId)
        {
            return await _context.OrderItems
                .Include(oi => oi.Order)
                .AnyAsync(oi => oi.Order != null
                    && oi.Order.UserId == userId
                    && oi.ProductId == productId
                    && oi.Order.Status == OrderStatus.Delivered);
        }

        public async Task<bool> HasUserReviewedProductAsync(string userId, int productId)
        {
            return await _dbSet
                .AnyAsync(r => r.UserId == userId && r.ProductId == productId);
        }

        public async Task<Review?> GetUserReviewForProductAsync(string userId, int productId)
        {
            return await _dbSet
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);
        }

        public async Task<double> GetAverageRatingAsync(int productId)
        {
            var reviews = await _dbSet
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            if (!reviews.Any()) return 0;

            return reviews.Average(r => r.Rating);
        }
    }
}

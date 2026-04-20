using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services.Interfaces;

namespace HDKTech.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepo;
        private readonly IProductRepository _productRepo;

        public ReviewService(IReviewRepository reviewRepo, IProductRepository productRepo)
        {
            _reviewRepo = reviewRepo;
            _productRepo = productRepo;
        }

        public async Task<(bool Success, string Message, Review? Review)> AddReviewAsync(int productId, string userId, int rating, string content)
        {
            if (string.IsNullOrEmpty(userId))
                return (false, "Vui lòng đăng nhập để đánh giá sản phẩm.", null);

            var product = await _productRepo.GetProductWithDetailsAsync(productId);
            if (product == null)
                return (false, "Sản phẩm không tồn tại.", null);

            if (rating < 1 || rating > 5)
                return (false, "Vui lòng chọn số sao từ 1 đến 5.", null);

            if (string.IsNullOrWhiteSpace(content) || content.Length < 10)
                return (false, "Nội dung đánh giá phải có ít nhất 10 ký tự.", null);

            var hasPurchased = await _reviewRepo.HasUserPurchasedProductAsync(userId, productId);
            if (!hasPurchased)
                return (false, "Bạn chỉ có thể đánh giá sản phẩm đã mua.", null);

            var existingReview = await _reviewRepo.HasUserReviewedProductAsync(userId, productId);
            if (existingReview)
                return (false, "Bạn đã đánh giá sản phẩm này rồi.", null);

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Content = content.Trim(),
                ReviewDate = DateTime.Now
            };

            await _reviewRepo.AddAsync(review);
            await _reviewRepo.SaveAsync();

            return (true, "Cảm ơn bạn đã đánh giá sản phẩm!", review);
        }

        public async Task<(bool Success, string Message)> UpdateReviewAsync(int reviewId, string userId, int rating, string content)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId);
            if (review == null)
                return (false, "Đánh giá không tồn tại.");

            if (review.UserId != userId)
                return (false, "Bạn không có quyền chỉnh sửa đánh giá này.");

            if (rating < 1 || rating > 5)
                return (false, "Vui lòng chọn số sao từ 1 đến 5.");

            if (string.IsNullOrWhiteSpace(content) || content.Length < 10)
                return (false, "Nội dung đánh giá phải có ít nhất 10 ký tự.");

            review.Rating = rating;
            review.Content = content.Trim();
            review.ReviewDate = DateTime.Now;

            await _reviewRepo.UpdateAsync(review);
            await _reviewRepo.SaveAsync();

            return (true, "Cập nhật đánh giá thành công!");
        }

        public async Task<(bool Success, string Message)> DeleteReviewAsync(int reviewId, string userId)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId);
            if (review == null)
                return (false, "Đánh giá không tồn tại.");

            if (review.UserId != userId)
                return (false, "Bạn không có quyền xóa đánh giá này.");

            await _reviewRepo.DeleteAsync(reviewId);
            await _reviewRepo.SaveAsync();

            return (true, "Xóa đánh giá thành công!");
        }

        public async Task<List<Review>> GetProductReviewsAsync(int productId)
        {
            return await _reviewRepo.GetByProductIdAsync(productId);
        }

        public async Task<bool> CanUserReviewAsync(string userId, int productId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            var hasPurchased = await _reviewRepo.HasUserPurchasedProductAsync(userId, productId);
            if (!hasPurchased)
                return false;

            var hasReviewed = await _reviewRepo.HasUserReviewedProductAsync(userId, productId);
            return !hasReviewed;
        }

        public async Task<Review?> GetUserReviewAsync(string userId, int productId)
        {
            return await _reviewRepo.GetUserReviewForProductAsync(userId, productId);
        }

        public async Task<double> GetAverageRatingAsync(int productId)
        {
            return await _reviewRepo.GetAverageRatingAsync(productId);
        }
    }
}

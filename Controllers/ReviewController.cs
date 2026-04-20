using HDKTech.Models;
using HDKTech.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDKTech.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add(int productId, int rating, string content)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá sản phẩm." });
            }

            var result = await _reviewService.AddReviewAsync(productId, userId, rating, content);

            if (result.Success)
            {
                _logger.LogInformation("User {UserId} added review for product {ProductId}", userId, productId);
                return Json(new { 
                    success = true, 
                    message = result.Message,
                    review = new {
                        result.Review!.Id,
                        result.Review.Rating,
                        result.Review.Content,
                        result.Review.ReviewDate,
                        UserName = User.Identity?.Name ?? "Khách hàng"
                    }
                });
            }

            return Json(new { success = false, message = result.Message });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Update(int reviewId, int rating, string content)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để chỉnh sửa đánh giá." });
            }

            var result = await _reviewService.UpdateReviewAsync(reviewId, userId, rating, content);

            if (result.Success)
            {
                _logger.LogInformation("User {UserId} updated review {ReviewId}", userId, reviewId);
            }

            return Json(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete(int reviewId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để xóa đánh giá." });
            }

            var result = await _reviewService.DeleteReviewAsync(reviewId, userId);

            if (result.Success)
            {
                _logger.LogInformation("User {UserId} deleted review {ReviewId}", userId, reviewId);
            }

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductReviews(int productId)
        {
            var reviews = await _reviewService.GetProductReviewsAsync(productId);
            var avgRating = await _reviewService.GetAverageRatingAsync(productId);

            return Json(new { 
                success = true, 
                reviews = reviews.Select(r => new {
                    r.Id,
                    r.Rating,
                    r.Content,
                    r.ReviewDate,
                    UserName = r.User?.FullName ?? "Ẩn danh"
                }),
                averageRating = avgRating,
                totalReviews = reviews.Count
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CheckCanReview(int productId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { canReview = false, message = "Vui lòng đăng nhập." });
            }

            var canReview = await _reviewService.CanUserReviewAsync(userId, productId);
            var existingReview = await _reviewService.GetUserReviewAsync(userId, productId);

            return Json(new { 
                canReview,
                hasReviewed = existingReview != null,
                existingReview = existingReview != null ? new {
                    existingReview.Id,
                    existingReview.Rating,
                    existingReview.Content
                } : null
            });
        }
    }
}

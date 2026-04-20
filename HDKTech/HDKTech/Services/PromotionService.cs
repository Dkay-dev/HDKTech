using HDKTech.Areas.Admin.Models;
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Services
{
    // ────────────────────────────────────────────────────────────────────────
    // Result DTO — trả về từ CalculateDiscountAsync
    // ────────────────────────────────────────────────────────────────────────
    public sealed class PromotionResult
    {
        /// <summary>True nếu mã hợp lệ và được áp dụng.</summary>
        public bool IsValid { get; init; }

        /// <summary>Số tiền được giảm (>= 0).</summary>
        public decimal DiscountAmount { get; init; }

        /// <summary>Phí ship sau khi áp (có thể = 0 nếu FreeShip).</summary>
        public decimal AdjustedShippingFee { get; init; }

        /// <summary>Thông báo trả về cho người dùng.</summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>Promotion entity (null nếu !IsValid).</summary>
        public Promotion? Promotion { get; init; }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Interface
    // ────────────────────────────────────────────────────────────────────────
    public interface IPromotionService
    {
        /// <summary>
        /// Validate và tính discount cho mã giảm giá.
        /// </summary>
        /// <param name="promoCode">Mã nhập từ người dùng.</param>
        /// <param name="userId">ID người dùng đang checkout.</param>
        /// <param name="subTotal">Tổng giá trị hàng (trước ship và giảm giá).</param>
        /// <param name="cartItems">Các item trong giỏ để kiểm tra scope.</param>
        /// <param name="originalShippingFee">Phí ship đã tính server-side.</param>
        Task<PromotionResult> CalculateDiscountAsync(
            string            promoCode,
            string            userId,
            decimal           subTotal,
            IEnumerable<CartItem> cartItems,
            decimal           originalShippingFee);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Implementation
    // ────────────────────────────────────────────────────────────────────────
    public class PromotionService : IPromotionService
    {
        private readonly HDKTechContext                 _context;
        private readonly ILogger<PromotionService>     _logger;

        public PromotionService(
            HDKTechContext             context,
            ILogger<PromotionService>  logger)
        {
            _context = context;
            _logger  = logger;
        }

        public async Task<PromotionResult> CalculateDiscountAsync(
            string                promoCode,
            string                userId,
            decimal               subTotal,
            IEnumerable<CartItem> cartItems,
            decimal               originalShippingFee)
        {
            // ── 0. Sanitize input ─────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(promoCode))
                return Fail("Vui lòng nhập mã giảm giá.");

            promoCode = promoCode.Trim().ToUpperInvariant();

            // ── 1. Tìm Promotion theo code ────────────────────────────────
            var promotion = await _context.Promotions
                .Include(p => p.PromotionProducts)
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.PromoCode != null &&
                    p.PromoCode.ToUpper() == promoCode &&
                    p.IsActive);

            if (promotion == null)
                return Fail("Mã giảm giá không tồn tại hoặc không còn hiệu lực.");

            // ── 2. Kiểm tra trạng thái & thời gian ───────────────────────
            var now = DateTime.Now;
            if (promotion.Status != PromotionStatus.Running)
                return Fail("Chương trình khuyến mãi chưa diễn ra hoặc đã kết thúc.");

            if (now < promotion.StartDate)
                return Fail($"Mã giảm giá có hiệu lực từ {promotion.StartDate:dd/MM/yyyy}.");

            if (now > promotion.EndDate)
                return Fail("Mã giảm giá đã hết hạn.");

            // ── 3. Kiểm tra giới hạn tổng lượt dùng ──────────────────────
            if (promotion.MaxUsageCount.HasValue &&
                promotion.UsageCount >= promotion.MaxUsageCount.Value)
                return Fail("Mã giảm giá đã đạt giới hạn lượt sử dụng.");

            // ── 4. Kiểm tra lượt dùng per-user ───────────────────────────
            if (promotion.MaxUsagePerUser.HasValue && promotion.MaxUsagePerUser.Value > 0)
            {
                var userUsageCount = await _context.OrderPromotions
                    .CountAsync(op =>
                        op.PromotionId == promotion.Id &&
                        op.Order != null && op.Order.UserId == userId);

                if (userUsageCount >= promotion.MaxUsagePerUser.Value)
                    return Fail("Bạn đã sử dụng hết lượt giảm giá cho mã này.");
            }

            // ── 5. Kiểm tra đơn tối thiểu ────────────────────────────────
            if (promotion.MinOrderAmount.HasValue && subTotal < promotion.MinOrderAmount.Value)
                return Fail(
                    $"Đơn hàng tối thiểu {promotion.MinOrderAmount.Value:N0}đ để áp mã này " +
                    $"(hiện tại: {subTotal:N0}đ).");

            // ── 6. Kiểm tra scope (sản phẩm/danh mục/brand) ──────────────
            if (!promotion.AppliesToAll && promotion.PromotionProducts.Any())
            {
                var scopeValid = await CheckScopeAsync(promotion, cartItems.ToList());
                if (!scopeValid)
                    return Fail("Mã giảm giá không áp dụng cho sản phẩm trong giỏ hàng của bạn.");
            }

            // ── 7. Tính discount theo loại ────────────────────────────────
            decimal discountAmount      = 0;
            decimal adjustedShippingFee = originalShippingFee;
            string  message;

            switch (promotion.PromotionType)
            {
                case PromotionType.Percentage:
                    discountAmount = subTotal * (promotion.Value / 100m);
                    if (promotion.MaxDiscountAmount.HasValue)
                        discountAmount = Math.Min(discountAmount, promotion.MaxDiscountAmount.Value);
                    discountAmount = Math.Round(discountAmount, 0);
                    message = $"Giảm {promotion.Value}% → -{discountAmount:N0}đ";
                    break;

                case PromotionType.FixedAmount:
                    discountAmount = Math.Min(promotion.Value, subTotal); // Không giảm quá subTotal
                    message = $"Giảm {discountAmount:N0}đ";
                    break;

                case PromotionType.FreeShip:
                    // FreeShip: Value = mức freeship tối đa (0 = miễn toàn bộ)
                    if (promotion.Value == 0)
                    {
                        discountAmount      = originalShippingFee;
                        adjustedShippingFee = 0;
                    }
                    else
                    {
                        discountAmount      = Math.Min(promotion.Value, originalShippingFee);
                        adjustedShippingFee = Math.Max(0, originalShippingFee - discountAmount);
                    }
                    message = adjustedShippingFee == 0
                        ? "Miễn phí vận chuyển"
                        : $"Giảm phí ship {discountAmount:N0}đ";
                    break;

                case PromotionType.FlashSale:
                    // FlashSale: cần xử lý ở cấp product; tại đây coi như Percentage
                    discountAmount = subTotal * (promotion.Value / 100m);
                    if (promotion.MaxDiscountAmount.HasValue)
                        discountAmount = Math.Min(discountAmount, promotion.MaxDiscountAmount.Value);
                    discountAmount = Math.Round(discountAmount, 0);
                    message = $"[Flash Sale] Giảm {promotion.Value}% → -{discountAmount:N0}đ";
                    break;

                default:
                    _logger.LogWarning(
                        "PromotionService: PromotionType không xử lý được: {Type}", promotion.PromotionType);
                    return Fail("Loại khuyến mãi không được hỗ trợ.");
            }

            _logger.LogInformation(
                "Promo '{Code}' áp cho User {UserId}: -{Discount}đ",
                promoCode, userId, discountAmount);

            return new PromotionResult
            {
                IsValid            = true,
                DiscountAmount     = discountAmount,
                AdjustedShippingFee = adjustedShippingFee,
                Message            = message,
                Promotion          = promotion
            };
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra xem có ít nhất 1 item trong giỏ thỏa scope của promotion không.
        /// Exclusion rule: nếu tất cả item đều bị exclude → scope không thỏa.
        /// </summary>
        private async Task<bool> CheckScopeAsync(
            Promotion         promotion,
            List<CartItem>    cartItems)
        {
            if (!cartItems.Any()) return false;

            var productIds  = cartItems.Select(i => i.ProductId).Distinct().ToList();
            var variantIds  = cartItems.Select(i => i.ProductVariantId).Distinct().ToList();

            // Lấy category và brand của các sản phẩm trong giỏ
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.CategoryId, p.BrandId })
                .ToListAsync();

            var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();
            var brandIds    = products.Where(p => p.BrandId.HasValue)
                                      .Select(p => p.BrandId!.Value).Distinct().ToList();

            bool anyIncluded = false;

            foreach (var pp in promotion.PromotionProducts)
            {
                if (pp.IsExclusion) continue; // Exclusion rule — xử lý sau

                bool matchesThisScope = pp.ScopeType switch
                {
                    PromotionScopeType.Product        => pp.ProductId.HasValue && productIds.Contains(pp.ProductId.Value),
                    PromotionScopeType.ProductVariant => pp.ProductVariantId.HasValue && variantIds.Contains(pp.ProductVariantId.Value),
                    PromotionScopeType.Category       => pp.CategoryId.HasValue && categoryIds.Contains(pp.CategoryId.Value),
                    PromotionScopeType.Brand          => pp.BrandId.HasValue && brandIds.Contains(pp.BrandId.Value),
                    _                                  => false
                };

                if (matchesThisScope) { anyIncluded = true; break; }
            }

            if (!anyIncluded) return false;

            // Kiểm tra exclusion: nếu tất cả item đều bị exclude → false
            var exclusions = promotion.PromotionProducts.Where(pp => pp.IsExclusion).ToList();
            if (!exclusions.Any()) return true;

            bool allExcluded = cartItems.All(item =>
            {
                var p = products.FirstOrDefault(pr => pr.Id == item.ProductId);
                return exclusions.Any(ex => ex.ScopeType switch
                {
                    PromotionScopeType.Product        => ex.ProductId == item.ProductId,
                    PromotionScopeType.ProductVariant => ex.ProductVariantId == item.ProductVariantId,
                    PromotionScopeType.Category       => p != null && ex.CategoryId == p.CategoryId,
                    PromotionScopeType.Brand          => p?.BrandId.HasValue == true && ex.BrandId == p.BrandId,
                    _                                  => false
                });
            });

            return !allExcluded;
        }

        private static PromotionResult Fail(string message) =>
            new() { IsValid = false, DiscountAmount = 0, Message = message };
    }
}

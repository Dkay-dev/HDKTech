using HDKTech.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Areas.Admin.Models
{
    public enum PromotionType
    {
        Percentage = 1,     // giảm X%
        FixedAmount = 2,    // giảm Y VND
        FreeShip = 3,       // miễn phí vận chuyển
        FlashSale = 4       // bán sốc trong khung giờ
    }

    public enum PromotionStatus
    {
        Draft = 0,
        Scheduled = 1,
        Running = 2,
        Paused = 3,
        Ended = 4,
        Archived = 5
    }

    /// <summary>
    /// Chiến dịch khuyến mãi. Một Promotion có thể áp dụng cho:
    ///  - Toàn đơn (không có PromotionProducts → default "All products").
    ///  - Sản phẩm / danh mục / thương hiệu cụ thể (qua bảng nối PromotionProducts).
    /// Mã giảm được snapshot vào OrderPromotion khi khách áp dụng, nên promotion
    /// có thể hết hạn / bị xoá mà không ảnh hưởng lịch sử đơn.
    /// </summary>
    [Table("Promotions")]
    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên chiến dịch không được để trống.")]
        [StringLength(200)]
        public string CampaignName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // ── Loại & giá trị ───────────────────────────────────────
        public PromotionType PromotionType { get; set; } = PromotionType.Percentage;

        /// <summary>
        /// Percentage: 10 = 10%.
        /// FixedAmount: 500000 = -500.000đ.
        /// FreeShip: giá trị tối đa được miễn (0 = miễn toàn bộ).
        /// FlashSale: giá bán cố định (thay vì % giảm).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        /// <summary>Số tiền đơn hàng tối thiểu để được áp (null = không yêu cầu).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderAmount { get; set; }

        /// <summary>Giảm tối đa (cho Percentage/FreeShip): null = không giới hạn.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        // ── Mã + lượt dùng ───────────────────────────────────────
        [StringLength(50)]
        public string? PromoCode { get; set; }      // null = auto-apply (flash sale)

        public int UsageCount { get; set; } = 0;
        public int? MaxUsageCount { get; set; }     // giới hạn toàn chiến dịch
        public int? MaxUsagePerUser { get; set; }   // giới hạn mỗi user

        // ── Thời gian ────────────────────────────────────────────
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // ── Trạng thái ───────────────────────────────────────────
        public bool IsActive { get; set; } = true;
        public PromotionStatus Status { get; set; } = PromotionStatus.Draft;

        /// <summary>True = áp cho toàn bộ sản phẩm (bỏ qua PromotionProducts).</summary>
        public bool AppliesToAll { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // ── Navigation ───────────────────────────────────────────
        public virtual ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
        public virtual ICollection<OrderPromotion> OrderPromotions { get; set; } = new List<OrderPromotion>();

        // ── Computed ─────────────────────────────────────────────
        [NotMapped]
        public bool IsCurrentlyActive =>
            IsActive
            && Status == PromotionStatus.Running
            && DateTime.Now >= StartDate
            && DateTime.Now <= EndDate
            && (MaxUsageCount == null || UsageCount < MaxUsageCount);

        /// <summary>Alias giữ tương thích với code UI cũ.</summary>
        [NotMapped]
        public string Name
        {
            get => CampaignName;
            set => CampaignName = value;
        }

        [NotMapped]
        public decimal DiscountPercent =>
            PromotionType == PromotionType.Percentage ? Value : 0;
    }
}

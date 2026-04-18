using HDKTech.Areas.Admin.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Lưu vết mã khuyến mãi đã áp dụng vào một đơn hàng.
    /// Toàn bộ dữ liệu Promotion tại thời điểm áp được SNAPSHOT để
    /// khi admin sửa/xoá Promotion sau này không làm sai lịch sử đơn.
    /// Một Order có thể có nhiều OrderPromotion (VD: 1 mã đơn hàng + 1 freeship).
    /// </summary>
    [Table("OrderPromotions")]
    public class OrderPromotion
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }

        /// <summary>FK tới Promotion — nullable: cho phép xoá Promotion mà không mất record.</summary>
        public int? PromotionId { get; set; }

        // ── Snapshot ─────────────────────────────────────────────
        [Required, StringLength(200)]
        public string CampaignNameSnapshot { get; set; } = string.Empty;

        [StringLength(50)]
        public string? PromoCodeSnapshot { get; set; }

        public PromotionType PromotionTypeSnapshot { get; set; }

        /// <summary>Value gốc của promotion (% hoặc số tiền).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValueSnapshot { get; set; }

        /// <summary>Số tiền thực tế được giảm trên đơn này.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.Now;

        // ── Navigation ───────────────────────────────────────────
        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        [ForeignKey(nameof(PromotionId))]
        public virtual Promotion? Promotion { get; set; }
    }
}

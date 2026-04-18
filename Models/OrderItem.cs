using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Chi tiết đơn hàng — luôn gắn với một ProductVariant cụ thể (SKU).
    ///
    /// Refactor:
    ///  - Thêm ProductVariantId (FK chính) ngoài ProductId (giữ để query nhanh).
    ///  - Thêm các SNAPSHOT fields: ProductNameSnapshot, SkuSnapshot, SpecSnapshot.
    ///    Nhờ đó khi admin sửa tên/xoá variant, lịch sử đơn hàng vẫn hiển thị đúng.
    ///  - Thêm DiscountAmount, LineTotal để tách giá gốc khỏi giá sau giảm.
    ///  - Thêm SerialNumber để trace từng máy cụ thể phục vụ bảo hành.
    ///  - OnDelete(Restrict) tới Product / Variant (cấu hình trong DbContext).
    /// </summary>
    [Table("OrderItems")]
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public int ProductVariantId { get; set; }

        // ── Snapshot (immutable sau khi đặt) ─────────────────────
        [Required, StringLength(200)]
        public string ProductNameSnapshot { get; set; } = string.Empty;

        [StringLength(64)]
        public string? SkuSnapshot { get; set; }

        [StringLength(500)]
        public string? SpecSnapshot { get; set; }       // "i7/16GB/512GB"

        // ── Số lượng & giá ───────────────────────────────────────
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }           // giá gốc tại thời điểm đặt

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }      // tổng giảm trên dòng này

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }           // = UnitPrice * Quantity - DiscountAmount

        // ── Bảo hành / truy xuất ─────────────────────────────────
        [StringLength(100)]
        public string? SerialNumber { get; set; }

        // ── Navigation ───────────────────────────────────────────
        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [ForeignKey(nameof(ProductVariantId))]
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Các lần bảo hành đã phát sinh trên máy này.</summary>
        public virtual ICollection<WarrantyClaim> WarrantyClaims { get; set; } = new List<WarrantyClaim>();
    }
}

using HDKTech.Areas.Admin.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Loại đối tượng mà dòng PromotionProduct trỏ tới.
    /// </summary>
    public enum PromotionScopeType
    {
        Product = 1,
        ProductVariant = 2,
        Category = 3,
        Brand = 4
    }

    /// <summary>
    /// Bảng nối n-n giữa Promotion và đối tượng được áp (Product / Variant /
    /// Category / Brand). Chỉ một trong các FK được set — tương ứng ScopeType.
    ///
    /// Cách thiết kế này tránh phải tạo 3-4 bảng nối riêng biệt và vẫn giữ
    /// được ràng buộc referential integrity nhờ FK có `Restrict`.
    /// </summary>
    [Table("PromotionProducts")]
    public class PromotionProduct
    {
        [Key]
        public int Id { get; set; }

        public int PromotionId { get; set; }

        public PromotionScopeType ScopeType { get; set; }

        public int? ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }

        /// <summary>
        /// True = exclude (loại trừ đối tượng này khỏi khuyến mãi); mặc định false = include.
        /// Cho phép logic kiểu "Áp dụng toàn category, trừ 2 sản phẩm X, Y".
        /// </summary>
        public bool IsExclusion { get; set; } = false;

        // ── Navigation ───────────────────────────────────────────
        [ForeignKey(nameof(PromotionId))]
        public virtual Promotion? Promotion { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [ForeignKey(nameof(ProductVariantId))]
        public virtual ProductVariant? Variant { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category? Category { get; set; }

        [ForeignKey(nameof(BrandId))]
        public virtual Brand? Brand { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Đại diện cho một biến thể (SKU) cụ thể của một dòng sản phẩm.
    /// Cùng một Product (vd: ThinkPad X1 Carbon Gen 11) có thể có nhiều Variant
    /// khác nhau về RAM / Storage / CPU / GPU / Color, mỗi Variant có giá và
    /// tồn kho riêng.
    ///
    /// Tách Variant khỏi Product để:
    ///  - Gom nhóm hiển thị (1 trang chi tiết, nhiều cấu hình chọn)
    ///  - Giá và khuyến mãi tính theo từng SKU (đúng 3NF)
    ///  - Tồn kho chi tiết theo SKU, không bị oversell cấu hình này khi hết hàng cấu hình khác.
    /// </summary>
    [Table("ProductVariants")]
    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        /// <summary>SKU duy nhất toàn hệ thống. VD: "X1C-G11-i7-16-512".</summary>
        [Required]
        [StringLength(64)]
        public string Sku { get; set; } = string.Empty;

        /// <summary>Tên hiển thị gợi nhớ cho biến thể. VD: "i7-13700H / 16GB / 512GB".</summary>
        [StringLength(200)]
        public string? VariantName { get; set; }

        // ── Thuộc tính kỹ thuật chính ─────────────────────────────
        [StringLength(100)] public string? Cpu { get; set; }
        [StringLength(50)]  public string? Ram { get; set; }       // "16GB"
        [StringLength(50)]  public string? Storage { get; set; }   // "512GB SSD"
        [StringLength(100)] public string? Gpu { get; set; }
        [StringLength(50)]  public string? Screen { get; set; }    // "14\" 2.8K OLED 120Hz"
        [StringLength(50)]  public string? Color { get; set; }
        [StringLength(50)]  public string? Os { get; set; }        // "Windows 11 Home"

        // ── Giá ───────────────────────────────────────────────────
        /// <summary>Giá bán hiện tại.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        /// <summary>Giá niêm yết (list price) để tính % giảm hiển thị.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ListPrice { get; set; }

        /// <summary>Giá vốn (COGS) — phục vụ báo cáo lãi/lỗ, không hiển thị công khai.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CostPrice { get; set; }

        // ── Trạng thái ────────────────────────────────────────────
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;   // biến thể mặc định hiển thị đầu tiên

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // ── Navigation ───────────────────────────────────────────
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // ── Computed (không map xuống DB) ────────────────────────
        [NotMapped]
        public int DiscountPercent
        {
            get
            {
                if (ListPrice.HasValue && ListPrice > Price && ListPrice > 0)
                    return (int)Math.Round((double)((ListPrice.Value - Price) / ListPrice.Value * 100));
                return 0;
            }
        }
    }
}

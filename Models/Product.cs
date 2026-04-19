using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Đại diện cho một DÒNG sản phẩm (model line) — KHÔNG phải một SKU cụ thể.
    ///
    /// Ví dụ: "ThinkPad X1 Carbon Gen 11" là một Product.
    /// Các cấu hình cụ thể ("i7/16GB/512GB", "i7/32GB/1TB") là các ProductVariant.
    ///
    /// Refactor so với bản cũ:
    ///  - Bỏ Price / ListPrice / FlashSale* / DiscountNote (chuyển sang ProductVariant
    ///    hoặc Promotion) để đúng 3NF.
    ///  - Không còn ICollection&lt;Inventory&gt; trực tiếp trên Product (Inventory
    ///    gắn với Variant). Muốn tính tổng tồn kho của Product → SUM qua Variants.
    /// </summary>
    [Table("Products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Slug dùng cho URL thân thiện SEO. Unique toàn hệ thống.</summary>
        [StringLength(220)]
        public string? Slug { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// Spec tổng quan dưới dạng HTML/Markdown để hiển thị tab "Thông số".
        /// Dữ liệu phục vụ filter đã được chuẩn hoá sang ProductTag.
        /// </summary>
        public string? Specifications { get; set; }

        public int CategoryId { get; set; }
        public int? BrandId { get; set; }

        /// <summary>Chính sách bảo hành mặc định cho cả dòng sản phẩm (nullable).</summary>
        public int? WarrantyPolicyId { get; set; }

        /// <summary>0 = Draft, 1 = Published, 2 = Archived.</summary>
        public int Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // ── Soft Delete (Module C) ────────────────────────────────
        /// <summary>
        /// Khi true, record được coi là đã xóa nhưng vẫn còn trong DB.
        /// Global Query Filter trong HDKTechContext sẽ tự lọc bỏ các record này.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        /// <summary>Thời điểm thực hiện soft delete.</summary>
        public DateTime? DeletedAt { get; set; }
        /// <summary>Username của người thực hiện xóa.</summary>
        public string? DeletedBy { get; set; }

        // ── Navigation ────────────────────────────────────────────
        [ForeignKey(nameof(CategoryId))]
        public virtual Category? Category { get; set; }

        [ForeignKey(nameof(BrandId))]
        public virtual Brand? Brand { get; set; }

        [ForeignKey(nameof(WarrantyPolicyId))]
        public virtual WarrantyPolicy? WarrantyPolicy { get; set; }

        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<ProductTag> Tags { get; set; } = new List<ProductTag>();

        // ── Convenience computed properties ──────────────────────
        /// <summary>Variant mặc định dùng để hiển thị giá / ảnh chính ở trang danh sách.</summary>
        [NotMapped]
        public ProductVariant? DefaultVariant =>
            Variants?.FirstOrDefault(v => v.IsDefault && v.IsActive)
            ?? Variants?.FirstOrDefault(v => v.IsActive);

        /// <summary>Giá hiển thị trên trang danh sách = giá của DefaultVariant.</summary>
        [NotMapped]
        public decimal? DisplayPrice => DefaultVariant?.Price;

        [NotMapped]
        public decimal? DisplayListPrice => DefaultVariant?.ListPrice;

        /// <summary>Tổng tồn kho khả dụng của tất cả biến thể.</summary>
        [NotMapped]
        public int TotalAvailableStock =>
            Variants?
                .SelectMany(v => v.Inventories ?? Enumerable.Empty<Inventory>())
                .Sum(i => i.AvailableQuantity) ?? 0;
    }
}
using System.ComponentModel.DataAnnotations;

namespace HDKTech.Models
{
    /// <summary>
    /// Đại diện cho một item trong giỏ hàng.
    ///
    /// Refactor:
    ///  - Thêm ProductVariantId: mỗi cấu hình chọn sẽ tạo 1 CartItem riêng.
    ///  - Thêm SkuSnapshot / SpecSnapshot: dùng lại khi ghi OrderItem.
    ///  - Price ở đây = giá variant tại thời điểm thêm vào giỏ.
    /// </summary>
    public class CartItem
    {
        [Key]
        public int ProductId { get; set; }

        /// <summary>FK tới ProductVariant (SKU). Bắt buộc kể từ sau refactor.</summary>
        public int ProductVariantId { get; set; }

        [Required, StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>Mã SKU tại thời điểm thêm vào giỏ.</summary>
        [StringLength(64)]
        public string? SkuSnapshot { get; set; }

        /// <summary>Mô tả cấu hình, vd "i7/16GB/512GB".</summary>
        [StringLength(500)]
        public string? SpecSnapshot { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(100)]
        public string? CategoryName { get; set; }

        public decimal TotalPrice => Price * Quantity;

        public CartItem() { }

        public CartItem(
            int productId,
            int productVariantId,
            string productName,
            decimal price,
            int quantity,
            string? skuSnapshot = null,
            string? specSnapshot = null,
            string? ImageUrl = null,
            string? categoryName = null)
        {
            ProductId = productId;
            ProductVariantId = productVariantId;
            ProductName = productName;
            Price = price;
            Quantity = quantity;
            SkuSnapshot = skuSnapshot;
            SpecSnapshot = specSnapshot;
            this.ImageUrl = ImageUrl;
            CategoryName = categoryName;
        }
    }
}

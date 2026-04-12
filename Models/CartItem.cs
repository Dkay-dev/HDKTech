using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Đại diện cho một item trong giỏ hàng
    /// Thiết kế linh hoạt: có thể map từ bất kỳ entity nào
    /// </summary>
    public class CartItem
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(100)]
        public string? CategoryName { get; set; }

        /// <summary>
        /// Tổng tiền của item này (Price * Quantity)
        /// </summary>
        public decimal TotalPrice => Price * Quantity;

        /// <summary>
        /// Constructor cho dễ dàng tạo CartItem từ Product hoặc model khác
        /// </summary>
        public CartItem() { }

        public CartItem(int productId, string productName, decimal price, int quantity, string? ImageUrl = null, string? categoryName = null)
        {
            ProductId = productId;
            ProductName = productName;
            Price = price;
            Quantity = quantity;
            ImageUrl = ImageUrl;
            CategoryName = categoryName;
        }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Cart item lưu trong Database — thay thế SessionCartService.
    /// Key xác định 1 dòng = (UserId hoặc GuestId, ProductId, ProductVariantId).
    /// Guest cart dùng GuestId (GUID lưu trong cookie), merge vào UserId sau khi login.
    /// </summary>
    [Table("CartItems")]
    public class DbCartItem
    {
        [Key]
        public int Id { get; set; }

        /// <summary>FK tới AppUser. Null nếu là guest chưa đăng nhập.</summary>
        [StringLength(450)]
        public string? UserId { get; set; }

        /// <summary>GUID lưu trong cookie cho guest user. Null nếu đã đăng nhập.</summary>
        [StringLength(100)]
        public string? GuestId { get; set; }

        public int ProductId { get; set; }
        public int ProductVariantId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [ForeignKey(nameof(ProductVariantId))]
        public virtual ProductVariant? Variant { get; set; }
    }
}

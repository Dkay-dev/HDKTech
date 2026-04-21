using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    public enum CheckoutStatus
    {
        Pending = 0,   // đang chờ thanh toán
        Paid    = 1,   // đã thanh toán, order đã tạo
        Failed  = 2,   // thanh toán thất bại
        Expired = 3    // hết hạn (quá 30 phút không thanh toán)
    }

    /// <summary>
    /// Thay thế TempData trong luồng checkout.
    /// Lưu toàn bộ thông tin đơn hàng vào DB với TTL 30 phút,
    /// tránh mất dữ liệu khi redirect qua payment gateway.
    ///
    /// Quan trọng: CartSnapshot lock giá tại thời điểm init checkout
    /// để tránh race condition nếu cart thay đổi mid-payment.
    /// </summary>
    [Table("PendingCheckouts")]
    public class PendingCheckout
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(450)]
        public string UserId { get; set; } = null!;

        // ── Thông tin người nhận ─────────────────────────────────
        [Required, StringLength(100)]
        public string RecipientName { get; set; } = null!;

        [Required, StringLength(20)]
        public string RecipientPhone { get; set; } = null!;

        [Required, StringLength(500)]
        public string ShippingAddress { get; set; } = null!;

        [StringLength(500)]
        public string? Note { get; set; }

        // ── Tổng tiền (server-calculated, không tin client) ──────
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // ── Phương thức thanh toán ───────────────────────────────
        [Required, StringLength(20)]
        public string PaymentMethod { get; set; } = "COD";  // COD / VNPAY / Momo

        /// <summary>
        /// MoMo orderId hoặc VNPay TxnRef gửi đến gateway.
        /// Dùng để đối soát khi callback về.
        /// </summary>
        [StringLength(100)]
        public string? GatewayOrderId { get; set; }

        // ── Cart snapshot (JSON) ─────────────────────────────────
        /// <summary>
        /// Serialize danh sách cart items TẠI THỜI ĐIỂM init checkout.
        /// Dùng để tạo OrderItems — KHÔNG đọc lại từ cart (cart có thể đã thay đổi).
        /// </summary>
        [Required]
        public string CartSnapshot { get; set; } = null!;

        // ── Trạng thái ───────────────────────────────────────────
        public CheckoutStatus Status { get; set; } = CheckoutStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
        public DateTime? PaidAt { get; set; }

        // Navigation
        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }
    }

    /// <summary>
    /// DTO snapshot của 1 cart item tại thời điểm checkout.
    /// Lưu trong PendingCheckout.CartSnapshot (JSON).
    /// </summary>
    public class CartItemSnapshot
    {
        public int     ProductId      { get; set; }
        public int     ProductVariantId { get; set; }
        public string  ProductName    { get; set; } = string.Empty;
        public string? SkuSnapshot    { get; set; }
        public string? SpecSnapshot   { get; set; }
        public string? ImageUrl       { get; set; }
        public decimal UnitPrice      { get; set; }  // giá lấy từ DB lúc checkout
        public int     Quantity       { get; set; }
        public decimal LineTotal      => UnitPrice * Quantity;
    }
}

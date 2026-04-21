using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Trạng thái đơn hàng (thay vì dùng int magic number).
    /// </summary>
    public enum OrderStatus
    {
        Pending = 0,       // vừa tạo, chờ xác nhận
        Confirmed = 1,     // đã xác nhận
        Packing = 2,       // đang đóng gói
        Shipping = 3,      // đang giao
        Delivered = 4,     // đã giao thành công
        Cancelled = 5,     // đã huỷ
        Returned = 6       // khách trả hàng
    }

    public enum PaymentStatus
    {
        Unpaid = 0,
        Paid = 1,
        Refunded = 2,
        Failed = 3
    }

    /// <summary>
    /// Đơn hàng — snapshot 100% dữ liệu giao hàng tại thời điểm đặt để
    /// lịch sử không phụ thuộc vào UserAddress hiện tại.
    /// </summary>
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string OrderCode { get; set; } = string.Empty;

        [Required, StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Địa chỉ gốc đã chọn lúc checkout (nullable vì user có thể đã xoá
        /// UserAddress sau đó; khi đó các field snapshot bên dưới vẫn giữ dữ liệu).
        /// </summary>
        public int? UserAddressId { get; set; }

        // ── Tổng tiền ────────────────────────────────────────────
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }          // tổng tiền hàng (trước giảm)

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }    // tổng giảm toàn đơn

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }       // = SubTotal - DiscountAmount + ShippingFee

        // ── Snapshot địa chỉ giao hàng ───────────────────────────
        [Required, StringLength(100)]
        public string RecipientName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string RecipientPhone { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string ShippingAddressLine { get; set; } = string.Empty;

        [StringLength(100)] public string ShippingWard { get; set; } = string.Empty;
        [StringLength(100)] public string ShippingDistrict { get; set; } = string.Empty;
        [StringLength(100)] public string ShippingCity { get; set; } = string.Empty;

        /// <summary>Địa chỉ full text để in hoá đơn (composed từ các field trên).</summary>
        [StringLength(1000)]
        public string? ShippingAddressFull { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        // ── Trạng thái ───────────────────────────────────────────
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        [StringLength(500)]
        public string? CancelReason { get; set; }

        // ── Thanh toán ───────────────────────────────────────────
        [StringLength(20)]
        public string PaymentMethod { get; set; } = "COD";       // COD / VNPAY / MOMO...

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        public DateTime? PaidAt { get; set; }

        // ── Navigation ───────────────────────────────────────────
        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }

        [ForeignKey(nameof(UserAddressId))]
        public virtual UserAddress? UserAddress { get; set; }

        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        public virtual ICollection<OrderPromotion> Promotions { get; set; } = new List<OrderPromotion>();

        public virtual Invoice? Invoice { get; set; }
    }
}

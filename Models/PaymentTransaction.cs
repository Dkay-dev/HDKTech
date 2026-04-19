using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    public enum PaymentGateway
    {
        Momo  = 1,
        VnPay = 2,
        COD   = 3
    }

    public enum TransactionStatus
    {
        Pending  = 0,
        Success  = 1,
        Failed   = 2,
        Refunded = 3
    }

    /// <summary>
    /// Lưu lại mỗi giao dịch thanh toán để implement idempotency.
    ///
    /// Idempotency hoạt động như sau:
    ///  - UNIQUE INDEX trên (GatewayTransactionId, Gateway)
    ///  - Khi callback/IPN đến, check trước xem đã có record chưa
    ///  - Nếu có → trả 200 ngay, không xử lý lại
    ///  - Nếu chưa → xử lý + insert record
    ///
    /// Điều này giải quyết vấn đề MoMo/VNPay gọi IPN nhiều lần.
    /// </summary>
    [Table("PaymentTransactions")]
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        public Guid PendingCheckoutId { get; set; }

        /// <summary>FK tới Order. Null cho đến khi CreateOrderAsync thành công.</summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// Transaction ID từ payment gateway:
        ///  - MoMo: giá trị field "orderId" gửi đến gateway (= PendingCheckout.GatewayOrderId)
        ///  - VNPay: vnp_TxnRef
        /// </summary>
        [Required, StringLength(100)]
        public string GatewayTransactionId { get; set; } = null!;

        public PaymentGateway Gateway { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        /// <summary>Log toàn bộ raw response từ gateway để debug/audit.</summary>
        public string? RawResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(PendingCheckoutId))]
        public virtual PendingCheckout? PendingCheckout { get; set; }
    }
}

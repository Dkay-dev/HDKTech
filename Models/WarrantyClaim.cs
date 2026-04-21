using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Trạng thái 1 lần yêu cầu bảo hành.
    /// </summary>
    public enum WarrantyClaimStatus
    {
        Received = 0,        // đã nhận máy, chờ chẩn đoán
        Diagnosing = 1,      // đang kiểm tra lỗi
        InRepair = 2,        // đang sửa / chờ linh kiện
        Completed = 3,       // đã sửa xong, chờ khách nhận
        Delivered = 4,       // đã trả máy cho khách
        Rejected = 5,        // từ chối bảo hành (hết hạn / lỗi do khách)
        Replaced = 6         // đổi máy mới
    }

    /// <summary>
    /// Một lần yêu cầu bảo hành (1 máy có thể có nhiều claim qua thời gian).
    ///
    /// Liên kết qua OrderItemId + SerialNumber để vừa trace được đơn hàng gốc
    /// (xác định ngày mua, còn hạn không) vừa định danh được máy cụ thể.
    /// </summary>
    [Table("WarrantyClaims")]
    public class WarrantyClaim
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Mã ticket bảo hành để hiển thị cho khách. VD: "WC-2026-0001".</summary>
        [Required, StringLength(30)]
        public string ClaimCode { get; set; } = string.Empty;

        public int OrderItemId { get; set; }

        /// <summary>
        /// Snapshot SerialNumber từ OrderItem tại thời điểm tạo claim —
        /// để claim vẫn đúng dù OrderItem.SerialNumber sau này bị sửa.
        /// </summary>
        [Required, StringLength(100)]
        public string SerialNumber { get; set; } = string.Empty;

        public DateTime ClaimDate { get; set; } = DateTime.Now;

        public WarrantyClaimStatus Status { get; set; } = WarrantyClaimStatus.Received;

        [Required, StringLength(1000)]
        public string IssueDescription { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? DiagnosisNote { get; set; }

        [StringLength(2000)]
        public string? Resolution { get; set; }

        /// <summary>Chi phí sửa (0 nếu trong diện bảo hành miễn phí).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal RepairCost { get; set; } = 0;

        /// <summary>True = khách chịu phí (ngoài bảo hành).</summary>
        public bool IsChargeable { get; set; } = false;

        // ── Thời gian xử lý ───────────────────────────────────────
        public DateTime? DiagnosedAt { get; set; }
        public DateTime? RepairStartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        /// <summary>userId của nhân viên xử lý (FK mềm tới AppUser).</summary>
        [StringLength(450)]
        public string? HandledBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(OrderItemId))]
        public virtual OrderItem? OrderItem { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Lý do phát sinh biến động tồn kho. Dùng enum tránh magic string.
    /// </summary>
    public enum StockMovementReason
    {
        Restock = 1,            // Nhập kho từ nhà cung cấp
        OrderReserved = 2,      // Giữ chỗ khi tạo đơn
        OrderShipped = 3,       // Trừ kho khi giao hàng
        OrderCancelled = 4,     // Hoàn lại kho khi huỷ đơn
        CustomerReturn = 5,     // Khách trả hàng
        Adjustment = 6,         // Kiểm kê điều chỉnh
        Damage = 7,             // Hư hỏng / mất mát
        Transfer = 8            // Chuyển kho
    }

    /// <summary>
    /// Nhật ký biến động tồn kho — mỗi dòng là một lần cộng/trừ kho.
    /// Đây là bảng append-only, KHÔNG BAO GIỜ update/delete để bảo đảm audit trail.
    /// Số tồn hiện tại của Inventory = SUM(Quantity) của tất cả StockMovement liên quan.
    /// </summary>
    [Table("StockMovements")]
    public class StockMovement
    {
        [Key]
        public long Id { get; set; }

        public int InventoryId { get; set; }

        /// <summary>
        /// Giá trị cộng (+) khi nhập, trả hàng; giá trị trừ (-) khi bán, hư hỏng.
        /// </summary>
        public int Quantity { get; set; }

        public StockMovementReason Reason { get; set; }

        /// <summary>Loại tài liệu tham chiếu: "Order", "PurchaseOrder", "Adjustment"...</summary>
        [StringLength(50)]
        public string? ReferenceType { get; set; }

        /// <summary>Id của tài liệu tham chiếu (VD: OrderId).</summary>
        public int? ReferenceId { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(450)]   // = IdentityUser.Id length
        public string? CreatedBy { get; set; }

        [ForeignKey(nameof(InventoryId))]
        public virtual Inventory? Inventory { get; set; }
    }
}

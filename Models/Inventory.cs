using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Tồn kho của một Variant tại một Warehouse cụ thể.
    ///
    /// Refactor so với bản cũ:
    ///  - PK là Id độc lập (không phải ProductId) → cho phép 1 Variant xuất hiện ở nhiều kho.
    ///  - Khoá FK chính là ProductVariantId (chi tiết SKU), ProductId giữ lại
    ///    như cột denormalized để query nhanh & không phá schema cũ hoàn toàn.
    ///  - ReservedQuantity: đơn đã đặt nhưng chưa giao (để tránh oversell).
    ///  - AvailableQuantity = Quantity - ReservedQuantity (computed, không map).
    /// </summary>
    [Table("Inventories")]
    public class Inventory
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int ProductVariantId { get; set; }

        /// <summary>
        /// Id kho (nullable để tương thích khi chưa bật multi-warehouse).
        /// Sau này có thể thêm entity Warehouse.
        /// </summary>
        public int? WarehouseId { get; set; }

        /// <summary>Số lượng vật lý tồn kho thực tế.</summary>
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        /// <summary>Số lượng đã được giữ chỗ cho các đơn chưa hoàn tất.</summary>
        [Range(0, int.MaxValue)]
        public int ReservedQuantity { get; set; }

        /// <summary>Ngưỡng cảnh báo hết hàng (low-stock alert).</summary>
        public int LowStockThreshold { get; set; } = 5;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public byte[]? RowVersion { get; set; }   // optimistic concurrency token

        // ── Navigation ─────────────────────────────────────────────
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [ForeignKey(nameof(ProductVariantId))]
        public virtual ProductVariant? Variant { get; set; }

        public virtual ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();

        // ── Computed ──────────────────────────────────────────────
        /// <summary>Số lượng có thể bán (physical - reserved).</summary>
        [NotMapped]
        public int AvailableQuantity => Math.Max(0, Quantity - ReservedQuantity);

        [NotMapped]
        public bool IsLowStock => AvailableQuantity <= LowStockThreshold;

        [NotMapped]
        public bool IsOutOfStock => AvailableQuantity <= 0;
    }
}

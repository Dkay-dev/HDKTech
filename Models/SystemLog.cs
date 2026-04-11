using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("SystemLogs")]
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Thời gian thực hiện hành động
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Tên người dùng thực hiện
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Username { get; set; }

        /// <summary>
        /// Loại hành động (Create, Update, Delete, Login, Logout, etc.)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ActionType { get; set; }

        /// <summary>
        /// Phần hành (Product, Order, Banner, KhuyenMai, Category, Brand, Role, etc.)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Module { get; set; }

        /// <summary>
        /// Mô tả chi tiết hành động
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// ID của entity bị ảnh hưởng (nếu có)
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Tên entity (tên sản phẩm, mã đơn hàng, etc.)
        /// </summary>
        [StringLength(500)]
        public string EntityName { get; set; }

        /// <summary>
        /// Giá trị cũ (nếu là Update) - JSON format
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// Giá trị mới (nếu là Update) - JSON format
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// Địa chỉ IP của người dùng
        /// </summary>
        [StringLength(50)]
        public string IpAddress { get; set; }

        /// <summary>
        /// User Agent / Browser Info
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Trạng thái hành động (Success, Failed, Pending)
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; } = "Success";

        /// <summary>
        /// Ghi chú lỗi (nếu có)
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Vai trò của người dùng
        /// </summary>
        [StringLength(100)]
        public string UserRole { get; set; }

        /// <summary>
        /// ID người dùng (nếu có)
        /// </summary>
        public string UserId { get; set; }
    }
}

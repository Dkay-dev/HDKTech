using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("ChatSessions")]
    public class ChatSession
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID của user đã đăng nhập. NULL nếu là khách vãng lai (guest).
        /// </summary>
        public string? CustomerId { get; set; }

        /// <summary>
        /// Tên hiển thị của khách vãng lai (khi CustomerId = null).
        /// </summary>
        [StringLength(100)]
        public string? GuestName { get; set; }

        /// <summary>
        /// Số điện thoại khách vãng lai (tùy chọn).
        /// </summary>
        [StringLength(20)]
        public string? GuestPhone { get; set; }

        /// <summary>
        /// Email khách vãng lai (tùy chọn, dùng để nhận diện lại session cũ).
        /// </summary>
        [StringLength(100)]
        public string? GuestEmail { get; set; }

        /// <summary>
        /// ID của staff đang hỗ trợ. NULL khi chưa có staff nhận.
        /// </summary>
        public string? StaffId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.Now;

        public DateTime? EndedAt { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [ForeignKey("CustomerId")]
        public virtual AppUser? Customer { get; set; }

        [ForeignKey("StaffId")]
        public virtual AppUser? Staff { get; set; }

        public virtual ICollection<ChatMessage> Messages { get; set; }

        /// <summary>
        /// Trả về tên hiển thị: FullName nếu đã đăng nhập, GuestName nếu là khách.
        /// </summary>
        [NotMapped]
        public string DisplayName =>
            Customer?.FullName
            ?? (string.IsNullOrWhiteSpace(GuestName) ? "Khách vãng lai" : GuestName);
    }
}

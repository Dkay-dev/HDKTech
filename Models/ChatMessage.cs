using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("ChatMessages")]
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int SessionId { get; set; }

        /// <summary>
        /// ID của người gửi (AppUser). NULL nếu người gửi là khách vãng lai.
        /// </summary>
        public string? SenderId { get; set; }

        /// <summary>
        /// Tên hiển thị của người gửi (lưu tại thời điểm gửi để không phụ thuộc FK).
        /// </summary>
        [StringLength(100)]
        public string? SenderName { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        [ForeignKey("SessionId")]
        public virtual ChatSession Session { get; set; }

        [ForeignKey("SenderId")]
        public virtual AppUser? Sender { get; set; }
    }
}

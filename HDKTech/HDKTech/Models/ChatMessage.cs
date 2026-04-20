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

        public string SenderId { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        [ForeignKey("SessionId")]
        public virtual ChatSession Session { get; set; }

        [ForeignKey("SenderId")]
        public virtual AppUser Sender { get; set; }
    }
}

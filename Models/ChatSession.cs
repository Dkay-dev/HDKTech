using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("ChatSessions")]
    public class ChatSession
    {
        [Key]
        public int Id { get; set; }

        public string CustomerId { get; set; }

        public string? StaffId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.Now;

        public DateTime? EndedAt { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [ForeignKey("CustomerId")]
        public virtual AppUser Customer { get; set; }

        [ForeignKey("StaffId")]
        public virtual AppUser Staff { get; set; }

        public virtual ICollection<ChatMessage> Messages { get; set; }
    }
}

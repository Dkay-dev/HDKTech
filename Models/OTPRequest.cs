using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("OTPRequests")]
    public class OTPRequest
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }

        [StringLength(6)]
        public string Code { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }
    }
}

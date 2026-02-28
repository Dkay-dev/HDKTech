using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("TinNhanChat")]
    public class TinNhanChat
    {
        [Key]
        public int MaTinNhan { get; set; }
        public int MaPhienChat { get; set; }
        public string MaNguoiGui { get; set; }
        [Required]
        public string NoiDung { get; set; }
        public DateTime ThoiGianGui { get; set; } = DateTime.Now;
        public bool DaXem { get; set; } = false;
        [ForeignKey("MaPhienChat")]
        public virtual PhienChat PhienChat { get; set; }
    }
}

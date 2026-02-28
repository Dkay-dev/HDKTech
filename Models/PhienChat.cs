using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("PhienChat")]

    public class PhienChat
    {
        [Key]
        public int MaPhienChat { get; set; }
        public string MaKhachHang { get; set; }
        public string MaNhanVien { get; set; }
        public DateTime NgayBatDau { get; set; } = DateTime.Now;
        public bool DaKetThuc { get; set; } = false;
        [ForeignKey("MaKhachHang")]
        public virtual NguoiDung KhachHang { get; set; }
        [ForeignKey("MaNhanVien")]
        public virtual NguoiDung NhanVien { get; set; }
        public virtual ICollection<TinNhanChat> TinNhanChats { get; set; }
    }
}

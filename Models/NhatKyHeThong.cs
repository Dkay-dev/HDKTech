using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("NhatKyHeThong")]
    public class NhatKyHeThong
    {
        [Key]
        public int MaNhatKy { get; set; }
        public string MaNguoiDung { get; set; }
        [Required]
        public string HanhDong { get; set; } // Sua, xoa
        public string LoaiDoiTuong { get; set; } // San Pham , Hoa Don
        public string MaDoiTuong { get; set; }
        public string NoiDungChiTiet { get; set; } // thay doi gia thanh

        public DateTime NgayThucHien { get; set; } = DateTime.Now;
        public string DiaChiIP { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung NguoiDung { get; set; }
    }
}

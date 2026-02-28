using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    public class HoaDon
    {
        [Key]
        public int HoaDonId { get; set; }

        public int MaDonHang { get; set; }
        public string TenCongTy { get; set; }
        public string MaSoThue { get; set; }
        public string DiaChiCongTy { get; set; }
        public string EmailNhanHoaDon { get; set; }

        public DateTime NgayYeuCau { get; set; } = DateTime.Now;

        [ForeignKey("MaDonHang")]
        public virtual DonHang DonHang { get; set; }
    }
}

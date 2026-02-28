using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("YeuCauOTP")]
    public class YeuCauOTP
    {
        [Key]
        public int MaOTP { get; set; }
        [Required]
        public string EmailHoacSDT { get; set; }
        [Required]
        public string MaOTPHash { get; set; }
        public DateTime ThoiGianHetHan { get; set; }
        public int SoLanGoSai { get; set; } = 0;
        public bool DaSuDung { get; set; } = false;
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public string LoaiOTP { get; set; } // quen mk, dky,..
    }
}

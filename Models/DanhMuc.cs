using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("DanhMuc")]
    public class DanhMuc
    {
        [Key]
        public int MaDanhMuc { get; set; }
        [Required(ErrorMessage = "Ten danh muc khong the null")]
        [StringLength(100)]
        public string TenDanhMuc { get; set; }
        public int? MaDanhMucCha { get; set; }

        public virtual ICollection<SanPham> SanPhams { get; set; }
        [ForeignKey("MaDanhMucCha")]
        public virtual DanhMuc DanhMucCha { get; set; }
        public virtual ICollection<DanhMuc> DanhMucCon { get; set; }
    }
}

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("Brands")]
    public class Brand
    {
        [Key]
        [DisplayName("Mã thương hiệu")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên thương hiệu không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự.")]
        [DisplayName("Tên thương hiệu")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        [DisplayName("Mô tả")]
        public string? Description { get; set; }

        // ── Soft Delete (Module C) ────────────────────────────────
        /// <summary>
        /// Khi true, thương hiệu được coi là đã xóa (ẩn khỏi mọi query thông thường).
        /// Global Query Filter trong HDKTechContext tự lọc bỏ.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

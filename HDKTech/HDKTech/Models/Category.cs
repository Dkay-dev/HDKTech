using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        [DisplayName("Mã danh mục")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
        [DisplayName("Tên danh mục")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        [DisplayName("Mô tả")]
        public string? Description { get; set; }

        [DisplayName("Hình ảnh banner")]
        [StringLength(500, ErrorMessage = "URL hình ảnh không được vượt quá 500 ký tự.")]
        public string? BannerImageUrl { get; set; }

        [DisplayName("Danh mục cha")]
        public int? ParentCategoryId { get; set; }

        // ── Soft Delete (Module C) ────────────────────────────────
        /// <summary>
        /// Khi true, danh mục được coi là đã xóa (ẩn khỏi mọi query thông thường).
        /// Global Query Filter trong HDKTechContext tự lọc bỏ.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        [ForeignKey("ParentCategoryId")]
        public virtual Category? ParentCategory { get; set; }

        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    }
}

// Models/ProductTag.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    /// <summary>
    /// Lightweight attribute store cho filtering.
    /// Giữ Specifications cho display, dùng ProductTag cho filter queries.
    /// </summary>
    [Table("ProductTags")]
    public class ProductTag
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        /// <summary>Ví dụ: "RAM", "CPU", "VGA", "Storage", "Screen"</summary>
        [Required]
        [StringLength(50)]
        public string TagKey { get; set; } = string.Empty;

        /// <summary>Ví dụ: "16GB", "Core i7-13700H", "RTX 4070", "1TB SSD"</summary>
        [Required]
        [StringLength(200)]
        public string TagValue { get; set; } = string.Empty;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}
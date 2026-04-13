using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("Brands")]
    public class Brand
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Brand name is required")]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }
}

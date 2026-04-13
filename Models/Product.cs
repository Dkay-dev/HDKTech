using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200)]
        public string Name { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string? Description { get; set; }
        public string? Specifications { get; set; }
        public int CategoryId { get; set; }
        public int BrandId { get; set; }
        public int Status { get; set; }
        public string? WarrantyInfo { get; set; } = "24 Months";
        public string? DiscountNote { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("BrandId")]
        public virtual Brand? Brand { get; set; }

        public virtual ICollection<OrderItem>? OrderItems { get; set; }
        public virtual ICollection<ProductImage>? Images { get; set; }
        public virtual ICollection<Inventory>? Inventories { get; set; }
        public virtual ICollection<Review>? Reviews { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ListPrice { get; set; }

        [NotMapped]
        public int DiscountPercent
        {
            get
            {
                if (ListPrice.HasValue && ListPrice > Price && ListPrice > 0)
                    return (int)Math.Round((double)((ListPrice - Price) / ListPrice * 100));
                return 0;
            }
        }

        /// <summary>
        /// Giá thực tế hiển thị cho khách hàng.
        /// Price đã là giá bán sau mọi điều chỉnh thủ công (ListPrice là giá niêm yết gốc).
        /// Dùng CurrentPrice nhất quán trên toàn hệ thống thay vì gọi trực tiếp Price.
        /// </summary>
        [NotMapped]
        public decimal CurrentPrice => Price;
    }
}

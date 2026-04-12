using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OrderCode { get; set; }

        public string UserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }

        public string RecipientName { get; set; }
        public string RecipientPhone { get; set; }
        public string ShippingAddress { get; set; }
        public int Status { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }

        public virtual ICollection<OrderItem> Items { get; set; }
        public virtual Invoice Invoice { get; set; }
    }
}

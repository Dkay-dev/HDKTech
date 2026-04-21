using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKTech.Models
{
    [Table("Invoices")]
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public string CompanyName { get; set; }
        public string TaxId { get; set; }
        public string CompanyAddress { get; set; }
        public string InvoiceEmail { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.Now;

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace HDKTech.Models
{
    public class CheckoutViewModel
    {
        public string PaymentMethod { get; set; } = "COD";

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ tên")]
        public string RecipientName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }

        [Display(Name = "Ghi chú đơn hàng")]
        public string GhiChu { get; set; }

        // Summary info (read-only)
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TongCong => TotalAmount + ShippingFee;
        public int SoProduct { get; set; }
        public List<CartItem> Items { get; set; } = new();
    }
}

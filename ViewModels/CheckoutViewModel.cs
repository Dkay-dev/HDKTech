using System.ComponentModel.DataAnnotations;
using HDKTech.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HDKTech.ViewModels
{
    public class CheckoutViewModel
    {
        public string PaymentMethod { get; set; } = "COD";

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự")]
        [Display(Name = "Họ tên")]
        public string RecipientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(200, ErrorMessage = "Email không được quá 200 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(
            @"^(0|\+84)(3[2-9]|5[6-9]|7[0|6-9]|8[0-9]|9[0-9])[0-9]{7}$",
            ErrorMessage = "Số điện thoại không hợp lệ (phải là số di động Việt Nam)")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Địa chỉ phải từ 10 đến 500 ký tự")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Ghi chú không được quá 500 ký tự")]
        [Display(Name = "Ghi chú đơn hàng")]
        public string? GhiChu { get; set; }

        [StringLength(50, ErrorMessage = "Mã giảm giá không được quá 50 ký tự")]
        [Display(Name = "Mã giảm giá")]
        public string? PromoCode { get; set; }

        [BindNever] public decimal ShippingFee  { get; set; }
        [BindNever] public decimal Discount     { get; set; }
        [BindNever] public decimal TotalAmount  { get; set; }
        [BindNever] public decimal TongCong     => TotalAmount + ShippingFee - Discount;
        [BindNever] public int     SoProduct    { get; set; }
        [BindNever] public List<CartItem> Items { get; set; } = new();
        [BindNever] public string? PromoMessage { get; set; }
    }
}

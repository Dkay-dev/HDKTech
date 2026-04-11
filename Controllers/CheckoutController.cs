using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Models;
using HDKTech.Repositories;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services;

namespace HDKTech.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartService _cartService;
        private readonly UserManager<NguoiDung> _userManager;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            IOrderRepository orderRepository,
            ICartService cartService,
            UserManager<NguoiDung> userManager,
            ILogger<CheckoutController> logger)
        {
            _orderRepository = orderRepository;
            _cartService = cartService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Checkout
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Lấy giỏ hàng từ session
            var cart = await _cartService.GetCartAsync();

            // Kiểm tra giỏ hàng có rỗng không
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước.";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy thông tin user hiện tại
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Checkout") });
            }

            // Tạo ViewModel với thông tin user auto-filled
            var viewModel = new CheckoutViewModel
            {
                TenNguoiNhan = user.HoTen ?? "",
                Email = user.Email ?? "",
                SoDienThoai = user.PhoneNumber ?? "",
                DiaChiGiaoHang = "", // Không có DiaChi trong NguoiDung
                Items = cart.Items,
                TongTien = (decimal)cart.Items.Sum(x => x.Price * x.Quantity),
                SoSanPham = cart.Items.Count,
                PhiVanChuyen = 0 // Có thể tính động dựa trên địa chỉ
            };

            return View(viewModel);
        }

        // POST: /Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            // Lấy giỏ hàng
            var cart = await _cartService.GetCartAsync();
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng trống.";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy user hiện tại
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            // Validate model
            if (!ModelState.IsValid)
            {
                // Nếu validate fail, trả lại form với dữ liệu cart
                model.Items = cart.Items;
                model.TongTien = (decimal)cart.Items.Sum(x => x.Price * x.Quantity);
                model.SoSanPham = cart.Items.Count;
                return View(model);
            }

            try
            {
                // Tạo đơn hàng
                var donHang = await _orderRepository.CreateOrderAsync(
                    userId: user.Id,
                    tenNguoiNhan: model.TenNguoiNhan,
                    soDienThoai: model.SoDienThoai,
                    diaChiGiaoHang: model.DiaChiGiaoHang,
                    items: cart.Items,
                    phiVanChuyen: model.PhiVanChuyen
                );

                // Cập nhật thông tin user
                user.HoTen = model.TenNguoiNhan;
                user.PhoneNumber = model.SoDienThoai;
                // user.DiaChi = model.DiaChiGiaoHang; // Không có property này
                await _userManager.UpdateAsync(user);

                // Xóa giỏ hàng
                await _cartService.ClearCartAsync();

                _logger.LogInformation($"Đơn hàng #{donHang.MaDonHangChuoi} được tạo thành công bởi user {user.Id}");

                // Chuyển sang trang success
                return RedirectToAction("Success", new { maDonHang = donHang.MaDonHangChuoi });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi tạo đơn hàng: {ex.Message}");
                TempData["Error"] = "Lỗi khi đặt hàng. Vui lòng thử lại.";

                // Trả lại form
                model.Items = cart.Items;
                model.TongTien = (decimal)cart.Items.Sum(x => x.Price * x.Quantity);
                model.SoSanPham = cart.Items.Count;
                return View(model);
            }
        }

        // GET: /Checkout/Success
        public async Task<IActionResult> Success(string maDonHang)
        {
            if (string.IsNullOrEmpty(maDonHang))
            {
                return RedirectToAction("Index", "Home");
            }

            var donHang = await _orderRepository.GetOrderByMaDonHangAsync(maDonHang);
            if (donHang == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            // ✅ Security Check: Verify user owns this order
            var user = await _userManager.GetUserAsync(User);
            if (user == null || donHang.MaNguoiDung != user.Id)
            {
                _logger.LogWarning($"Unauthorized access attempt to order {maDonHang} by user {user?.Id}");
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("Index", "Home");
            }

            return View(donHang);
        }
    }
}

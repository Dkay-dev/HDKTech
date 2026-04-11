using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    [Route("admin/[controller]")]
    public class OrderController : Controller
    {
        private readonly HDKTechContext _context;
        private readonly ILogger<OrderController> _logger;

        public OrderController(HDKTechContext context, ILogger<OrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Display all orders with filtering and sorting
        /// GET: /admin/order
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20, string searchTerm = "", int statusFilter = -1, string sortBy = "date")
        {
            try
            {
                IQueryable<DonHang> query = _context.DonHangs
                    .AsNoTracking()
                    .Include(o => o.NguoiDung)
                    .Include(o => o.ChiTietDonHangs);

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(o => 
                        o.MaDonHangChuoi.Contains(searchTerm) ||
                        o.NguoiDung.HoTen.Contains(searchTerm) ||
                        o.SoDienThoaiNhan.Contains(searchTerm));
                }

                // Apply status filter
                if (statusFilter >= 0)
                {
                    query = query.Where(o => o.TrangThaiDonHang == statusFilter);
                }

                // Apply sorting
                query = sortBy switch
                {
                    "amount_high" => query.OrderByDescending(o => o.TongTien),
                    "amount_low" => query.OrderBy(o => o.TongTien),
                    "customer" => query.OrderBy(o => o.NguoiDung.HoTen),
                    _ => query.OrderByDescending(o => o.NgayDatHang)
                };

                var totalCount = await query.CountAsync();
                var orders = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.Orders = orders;
                ViewBag.TotalCount = totalCount;
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.SearchTerm = searchTerm;
                ViewBag.StatusFilter = statusFilter;
                ViewBag.SortBy = sortBy;

                // Summary statistics
                var pendingCount = await _context.DonHangs.AsNoTracking().CountAsync(o => o.TrangThaiDonHang == 0);
                var processingCount = await _context.DonHangs.AsNoTracking().CountAsync(o => o.TrangThaiDonHang == 1);
                var shippingCount = await _context.DonHangs.AsNoTracking().CountAsync(o => o.TrangThaiDonHang == 2);
                var deliveredCount = await _context.DonHangs.AsNoTracking().CountAsync(o => o.TrangThaiDonHang == 3);
                var cancelledCount = await _context.DonHangs.AsNoTracking().CountAsync(o => o.TrangThaiDonHang == 4);

                ViewBag.PendingCount = pendingCount;
                ViewBag.ProcessingCount = processingCount;
                ViewBag.ShippingCount = shippingCount;
                ViewBag.DeliveredCount = deliveredCount;
                ViewBag.CancelledCount = cancelledCount;

                // Today's statistics
                var today = DateTime.Now.Date;
                var todayOrders = await _context.DonHangs
                    .AsNoTracking()
                    .Where(o => o.NgayDatHang.Date == today)
                    .ToListAsync();

                ViewBag.TodayOrderCount = todayOrders.Count;
                ViewBag.TodayRevenue = todayOrders.Sum(o => o.TongTien);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["Error"] = "Lỗi khi tải danh sách đơn hàng";
                return View();
            }
        }

        /// <summary>
        /// Display order details
        /// GET: /admin/order/details/5
        /// </summary>
        [HttpGet]
        [Route("details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _context.DonHangs
                    .Include(o => o.NguoiDung)
                    .Include(o => o.ChiTietDonHangs)
                        .ThenInclude(od => od.SanPham)
                    .FirstOrDefaultAsync(o => o.MaDonHang == id);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("Index");
                }

                ViewBag.Order = order;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details");
                TempData["Error"] = "Lỗi khi tải chi tiết đơn hàng";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Update order status
        /// POST: /admin/order/update-status
        /// </summary>
        [HttpPost]
        [Route("update-status")]
        public async Task<IActionResult> UpdateStatus(int orderId, int newStatus)
        {
            try
            {
                var order = await _context.DonHangs.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                order.TrangThaiDonHang = newStatus;
                _context.DonHangs.Update(order);
                await _context.SaveChangesAsync();

                var statusName = GetStatusName(newStatus);
                return Json(new { success = true, message = $"Cập nhật trạng thái thành '{statusName}' thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái" });
            }
        }

        /// <summary>
        /// Cancel order
        /// POST: /admin/order/cancel
        /// </summary>
        [HttpPost]
        [Route("cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                var order = await _context.DonHangs.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (order.TrangThaiDonHang == 3) // Delivered
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng đã giao" });
                }

                order.TrangThaiDonHang = 4; // Cancelled
                _context.DonHangs.Update(order);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order");
                return Json(new { success = false, message = "Lỗi khi hủy đơn hàng" });
            }
        }

        /// <summary>
        /// Export orders to CSV
        /// GET: /admin/order/export
        /// </summary>
        [HttpGet]
        [Route("export")]
        public async Task<IActionResult> Export(string searchTerm = "", int statusFilter = -1)
        {
            try
            {
                IQueryable<DonHang> query = _context.DonHangs
                    .AsNoTracking()
                    .Include(o => o.NguoiDung);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(o => 
                        o.MaDonHangChuoi.Contains(searchTerm) ||
                        o.NguoiDung.HoTen.Contains(searchTerm));
                }

                if (statusFilter >= 0)
                {
                    query = query.Where(o => o.TrangThaiDonHang == statusFilter);
                }

                var orders = await query.OrderByDescending(o => o.NgayDatHang).ToListAsync();

                var csv = "Mã Đơn Hàng,Khách Hàng,Số Điện Thoại,Địa Chỉ,Tổng Tiền,Phí Vận Chuyển,Trạng Thái,Ngày Đặt\n";

                foreach (var order in orders)
                {
                    var status = GetStatusName(order.TrangThaiDonHang);
                    csv += $"\"{order.MaDonHangChuoi}\",\"{order.TenNguoiNhan}\",\"{order.SoDienThoaiNhan}\",\"{order.DiaChiGiaoHang}\",{order.TongTien},{order.PhiVanChuyen},\"{status}\",{order.NgayDatHang:yyyy-MM-dd}\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", $"orders_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting orders");
                TempData["Error"] = "Lỗi khi xuất dữ liệu";
                return RedirectToAction("Index");
            }
        }

        private string GetStatusName(int status)
        {
            return status switch
            {
                0 => "Chờ xác nhận",
                1 => "Đang xử lý",
                2 => "Đang giao",
                3 => "Đã giao",
                4 => "Đã hủy",
                _ => "Không xác định"
            };
        }
    }
}

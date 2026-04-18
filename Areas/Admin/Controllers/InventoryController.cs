using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Services;

namespace HDKTech.Areas.Admin.Controllers
{
    /// <summary>
    /// InventoryController — Quản lý kho hàng (Sprint 1).
    /// Tách biệt hoàn toàn với ProductController.
    /// Phục vụ nút "Quản lý kho" trên Dashboard.
    ///
    /// Bảng dữ liệu: Inventories (ProductId, Quantity, UpdatedAt)
    /// AuditLog    : SystemLogs via ISystemLogService
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = "RequireManager")]
    [Route("admin/inventory")]
    public class InventoryController : Controller
    {
        private readonly HDKTechContext   _db;
        private readonly ISystemLogService _logService;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(
            HDKTechContext            db,
            ISystemLogService         logService,
            ILogger<InventoryController> logger)
        {
            _db         = db;
            _logService = logService;
            _logger     = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // INDEX — Bảng tồn kho với Inline Edit
        // GET /admin/inventory
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(
            string search   = "",
            int    page     = 1,
            int    pageSize = 25)
        {
            try
            {
                // Query Products JOIN Variants JOIN Inventories (1 product → n variant → n inventory)
                var query = _db.Products
                    .AsNoTracking()
                    .Include(p => p.Variants).ThenInclude(v => v.Inventories)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(p => p.Name.Contains(search));

                var totalCount = await query.CountAsync();

                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var items = products.Select(p =>
                {
                    var inventories = p.Variants?
                        .SelectMany(v => v.Inventories ?? Enumerable.Empty<Inventory>())
                        .ToList() ?? new List<Inventory>();

                    return new InventoryViewModel
                    {
                        ProductId   = p.Id,
                        ProductName = p.Name,
                        SKU         = p.DefaultVariant?.Sku ?? $"P-{p.Id:D5}",
                        Stock       = inventories.Sum(i => i.Quantity),
                        UpdatedAt   = inventories.Any()
                                        ? inventories.Max(i => i.UpdatedAt)
                                        : p.CreatedAt
                    };
                }).ToList();

                ViewBag.Search     = search;
                ViewBag.Page       = page;
                ViewBag.PageSize   = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return View(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory list");
                TempData["Error"] = "Lỗi khi tải danh sách kho hàng";
                return View(new List<InventoryViewModel>());
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE STOCK — Cập nhật tồn kho + ghi AuditLog
        // POST /admin/inventory/update-stock
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Cập nhật nhanh số lượng tồn kho (Inline Edit, AJAX).
        /// Ghi AuditLog chi tiết: số lượng cũ → mới, ai thay đổi, khi nào.
        /// </summary>
        [HttpPost("update-stock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(
            [FromForm] int productId,
            [FromForm] int newStock)
        {
            if (newStock < 0)
                return Json(new { success = false, message = "Số lượng không được âm." });

            try
            {
                // Kiểm tra product tồn tại + default variant
                var product = await _db.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

                var defaultVariant = product.Variants?
                    .FirstOrDefault(v => v.IsDefault)
                    ?? product.Variants?.FirstOrDefault();

                if (defaultVariant == null)
                    return Json(new { success = false, message = "Sản phẩm chưa có cấu hình nào — hãy tạo Variant trước." });

                var inventory = await _db.Inventories
                    .FirstOrDefaultAsync(i => i.ProductVariantId == defaultVariant.Id);

                int oldStock;

                if (inventory == null)
                {
                    oldStock  = 0;
                    inventory = new Inventory
                    {
                        ProductId        = productId,
                        ProductVariantId = defaultVariant.Id,
                        Quantity         = newStock,
                        UpdatedAt        = DateTime.Now
                    };
                    _db.Inventories.Add(inventory);
                }
                else
                {
                    oldStock            = inventory.Quantity;
                    inventory.Quantity  = newStock;
                    inventory.UpdatedAt = DateTime.Now;
                    _db.Inventories.Update(inventory);
                }

                await _db.SaveChangesAsync();

                // ── Ghi AuditLog vào SystemLogs ──────────────────────────────
                var delta = newStock - oldStock;
                await _logService.LogActionAsync(
                    username:    User.Identity?.Name ?? "Admin",
                    actionType:  "Update",
                    module:      "Inventory",
                    description: $"Cập nhật tồn kho '{product.Name}' (SKU: P-{productId:D5}): " +
                                 $"{oldStock} → {newStock} " +
                                 $"({(delta >= 0 ? "+" : "")}{delta})",
                    entityId:    productId.ToString(),
                    entityName:  product.Name,
                    oldValue:    oldStock.ToString(),
                    newValue:    newStock.ToString(),
                    userRole:    User.IsInRole("Admin") ? "Admin" : "Manager"
                );
                // ─────────────────────────────────────────────────────────────

                _logger.LogInformation(
                    "Inventory updated: Product {Id} ({Name}) {Old}→{New} by {User}",
                    productId, product.Name, oldStock, newStock, User.Identity?.Name);

                return Json(new
                {
                    success   = true,
                    message   = "Cập nhật tồn kho thành công",
                    newStock,
                    updatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
                return Json(new { success = false, message = "Lỗi khi cập nhật tồn kho." });
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ViewModel (scope nội bộ, không cần file riêng)
    // ─────────────────────────────────────────────────────────────────────────
    public class InventoryViewModel
    {
        public int      ProductId   { get; set; }
        public string   ProductName { get; set; } = string.Empty;
        public string   SKU         { get; set; } = string.Empty;
        public int      Stock       { get; set; }
        public DateTime UpdatedAt   { get; set; }
    }
}

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
    public class BrandController : Controller
    {
        private readonly HDKTechContext _context;
        private readonly ILogger<BrandController> _logger;

        public BrandController(HDKTechContext context, ILogger<BrandController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Display all brands with search and filter
        /// GET: /admin/brand
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index(string searchTerm = "", int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                IQueryable<HangSX> query = _context.HangSXs
                    .AsNoTracking()
                    .Include(b => b.SanPhams);

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(b => 
                        b.TenHangSX.Contains(searchTerm) ||
                        b.MoTa.Contains(searchTerm));
                }

                var totalCount = await query.CountAsync();
                var brands = await query
                    .OrderBy(b => b.TenHangSX)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.Brands = brands;
                ViewBag.TotalCount = totalCount;
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.SearchTerm = searchTerm;

                // Summary statistics
                var totalBrands = await _context.HangSXs.AsNoTracking().CountAsync();
                var totalProducts = await _context.SanPhams.AsNoTracking().CountAsync();
                var brandsWithoutProducts = await _context.HangSXs
                    .AsNoTracking()
                    .Where(b => b.SanPhams.Count == 0)
                    .CountAsync();

                ViewBag.TotalBrands = totalBrands;
                ViewBag.TotalProducts = totalProducts;
                ViewBag.BrandsWithoutProducts = brandsWithoutProducts;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading brands");
                TempData["Error"] = "Lỗi khi tải danh sách thương hiệu";
                return View();
            }
        }

        /// <summary>
        /// Display brand details with products
        /// GET: /admin/brand/details/5
        /// </summary>
        [HttpGet]
        [Route("details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var brand = await _context.HangSXs
                    .Include(b => b.SanPhams)
                        .ThenInclude(p => p.HinhAnhs)
                    .Include(b => b.SanPhams)
                        .ThenInclude(p => p.KhoHangs)
                    .FirstOrDefaultAsync(b => b.MaHangSX == id);

                if (brand == null)
                {
                    TempData["Error"] = "Không tìm thấy thương hiệu";
                    return RedirectToAction("Index");
                }

                ViewBag.Brand = brand;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading brand details");
                TempData["Error"] = "Lỗi khi tải chi tiết thương hiệu";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Create new brand
        /// GET: /admin/brand/create
        /// </summary>
        [HttpGet]
        [Route("create")]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Save new brand
        /// POST: /admin/brand/create
        /// </summary>
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create(HangSX brand)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(brand);
                }

                _context.HangSXs.Add(brand);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Tạo thương hiệu '{brand.TenHangSX}' thành công";
                return RedirectToAction("Details", new { id = brand.MaHangSX });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating brand");
                TempData["Error"] = "Lỗi khi tạo thương hiệu";
                return View(brand);
            }
        }

        /// <summary>
        /// Edit brand
        /// GET: /admin/brand/edit/5
        /// </summary>
        [HttpGet]
        [Route("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var brand = await _context.HangSXs.FindAsync(id);
                if (brand == null)
                {
                    TempData["Error"] = "Không tìm thấy thương hiệu";
                    return RedirectToAction("Index");
                }

                ViewBag.Brand = brand;
                return View(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page");
                TempData["Error"] = "Lỗi khi tải trang chỉnh sửa";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Update brand
        /// POST: /admin/brand/edit/5
        /// </summary>
        [HttpPost]
        [Route("edit/{id}")]
        public async Task<IActionResult> Edit(int id, HangSX brand)
        {
            try
            {
                if (id != brand.MaHangSX)
                {
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    return View(brand);
                }

                _context.HangSXs.Update(brand);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Cập nhật thương hiệu '{brand.TenHangSX}' thành công";
                return RedirectToAction("Details", new { id = brand.MaHangSX });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating brand");
                TempData["Error"] = "Lỗi khi cập nhật thương hiệu";
                return View(brand);
            }
        }

        /// <summary>
        /// Delete brand
        /// POST: /admin/brand/delete
        /// </summary>
        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> Delete(int brandId)
        {
            try
            {
                var brand = await _context.HangSXs.FindAsync(brandId);
                if (brand == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thương hiệu" });
                }

                // Check if brand has products
                var productCount = await _context.SanPhams
                    .CountAsync(p => p.MaHangSX == brandId);

                if (productCount > 0)
                {
                    return Json(new { success = false, message = $"Không thể xóa thương hiệu có {productCount} sản phẩm" });
                }

                _context.HangSXs.Remove(brand);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Xóa thương hiệu '{brand.TenHangSX}' thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting brand");
                return Json(new { success = false, message = "Lỗi khi xóa thương hiệu" });
            }
        }

        /// <summary>
        /// Export brands to CSV
        /// GET: /admin/brand/export
        /// </summary>
        [HttpGet]
        [Route("export")]
        public async Task<IActionResult> Export(string searchTerm = "")
        {
            try
            {
                IQueryable<HangSX> query = _context.HangSXs
                    .AsNoTracking()
                    .Include(b => b.SanPhams);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(b => 
                        b.TenHangSX.Contains(searchTerm) ||
                        b.MoTa.Contains(searchTerm));
                }

                var brands = await query.OrderBy(b => b.TenHangSX).ToListAsync();

                var csv = "Tên Thương Hiệu,Mô Tả,Số Sản Phẩm\n";

                foreach (var brand in brands)
                {
                    var description = brand.MoTa?.Replace("\"", "\"\"") ?? "";
                    var productCount = brand.SanPhams?.Count ?? 0;
                    csv += $"\"{brand.TenHangSX}\",\"{description}\",{productCount}\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", $"brands_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting brands");
                TempData["Error"] = "Lỗi khi xuất dữ liệu";
                return RedirectToAction("Index");
            }
        }
    }
}

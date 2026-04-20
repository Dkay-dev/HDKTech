using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminArea")]
    [Route("admin/[controller]")]
    public class BrandController : Controller
    {
        private readonly IBrandRepository _brandRepo;
        private readonly ILogger<BrandController> _logger;

        public BrandController(IBrandRepository brandRepo, ILogger<BrandController> logger)
        {
            _brandRepo = brandRepo;
            _logger = logger;
        }

        // GET: /admin/brand
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(string searchTerm = "", int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var all = await _brandRepo.GetAllWithProductCountAsync();
                var query = string.IsNullOrWhiteSpace(searchTerm)
                    ? all
                    : all.Where(b =>
                        b.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (b.Description != null && b.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                      .ToList();

                var totalCount = query.Count;
                var paged = query.OrderBy(b => b.Name).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.Brands                = paged;
                ViewBag.TotalCount            = totalCount;
                ViewBag.PageNumber            = pageNumber;
                ViewBag.PageSize              = pageSize;
                ViewBag.TotalPages            = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.SearchTerm            = searchTerm;
                ViewBag.TotalBrands           = await _brandRepo.CountAsync();
                ViewBag.TotalProducts         = all.Sum(b => b.Products?.Count ?? 0);
                ViewBag.BrandsWithoutProducts = await _brandRepo.CountEmptyAsync();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải danh sách thương hiệu");
                TempData["Error"] = "Lỗi khi tải danh sách thương hiệu.";
                return View();
            }
        }

        // GET: /admin/brand/details/5
        [HttpGet("details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var brand = await _brandRepo.GetByIdWithProductsAsync(id);
            if (brand == null) { TempData["Error"] = "Không tìm thấy thương hiệu."; return RedirectToAction(nameof(Index)); }
            ViewBag.Brand = brand;
            return View(brand);
        }

        // GET: /admin/brand/create
        [HttpGet("create")]
        public IActionResult Create() => View(new Brand());

        // POST: /admin/brand/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand)
        {
            if (!ModelState.IsValid) return View(brand);
            try
            {
                if (await _brandRepo.AddAsync(brand))
                {
                    TempData["Success"] = $"Tạo thương hiệu \"{brand.Name}\" thành công!";
                    return RedirectToAction(nameof(Details), new { id = brand.Id });
                }
                TempData["Error"] = "Không thể lưu. Vui lòng thử lại.";
                return View(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo brand: {Name}", brand.Name);
                TempData["Error"] = "Lỗi khi tạo thương hiệu.";
                return View(brand);
            }
        }

        // GET: /admin/brand/edit/5
        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _brandRepo.GetByIdAsync(id);
            if (brand == null) { TempData["Error"] = "Không tìm thấy thương hiệu."; return RedirectToAction(nameof(Index)); }
            return View(brand);
        }

        // POST: /admin/brand/edit/5
        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand brand)
        {
            if (id != brand.Id) return NotFound();
            if (!ModelState.IsValid) return View(brand);
            try
            {
                if (await _brandRepo.UpdateAsync(brand))
                {
                    TempData["Success"] = $"Cập nhật thương hiệu \"{brand.Name}\" thành công!";
                    return RedirectToAction(nameof(Details), new { id = brand.Id });
                }
                TempData["Error"] = "Không thể cập nhật. Vui lòng thử lại.";
                return View(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật brand Id: {Id}", id);
                TempData["Error"] = "Lỗi khi cập nhật thương hiệu.";
                return View(brand);
            }
        }

        // POST: /admin/brand/delete (AJAX)
        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int brandId)
        {
            try
            {
                var brand = await _brandRepo.GetByIdAsync(brandId);
                if (brand == null) return Json(new { success = false, message = "Không tìm thấy thương hiệu." });

                // Fix bug: kiểm tra BrandId, không phải Id
                if (await _brandRepo.HasProductsAsync(brandId))
                    return Json(new { success = false, message = $"Không thể xóa \"{brand.Name}\" vì đang có sản phẩm liên kết." });

                if (await _brandRepo.DeleteAsync(brandId))
                    return Json(new { success = true, message = $"Đã xóa thương hiệu \"{brand.Name}\"." });

                return Json(new { success = false, message = "Không thể xóa. Vui lòng thử lại." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa brand Id: {Id}", brandId);
                return Json(new { success = false, message = "Lỗi khi xóa thương hiệu." });
            }
        }

        // GET: /admin/brand/export
        [HttpGet("export")]
        public async Task<IActionResult> Export(string searchTerm = "")
        {
            var brands = await _brandRepo.GetAllWithProductCountAsync();
            var query = string.IsNullOrWhiteSpace(searchTerm) ? brands
                : brands.Where(b => b.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            var csv = "Mã,Tên,Mô Tả,Số Sản Phẩm\n";
            foreach (var b in query.OrderBy(x => x.Name))
                csv += $"{b.Id},\"{b.Name}\",\"{b.Description?.Replace("\"", "\"\"") ?? ""}\",{b.Products?.Count ?? 0}\n";
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"brands_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý Danh Mục sản phẩm trong khu vực Admin.
    /// Sử dụng Repository Pattern qua ICategoryRepository.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    [Route("admin/[controller]")]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepo;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategoryRepository categoryRepo, ILogger<CategoryController> logger)
        {
            _categoryRepo = categoryRepo;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════
        // INDEX - Danh sách danh mục
        // GET: /admin/category
        // GET: /admin/category/index
        // ══════════════════════════════════════════════════════════════
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(string searchTerm = "", int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                // Lấy toàn bộ danh mục kèm chi tiết
                var allCategories = await _categoryRepo.GetAllWithDetailsAsync();

                // Lọc chỉ lấy danh mục gốc (không có cha)
                var query = allCategories.Where(c => c.ParentCategoryId == null).AsQueryable();

                // Áp dụng tìm kiếm
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim();
                    query = query.Where(c =>
                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (c.Description != null && c.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var filtered = query.OrderBy(c => c.Name).ToList();
                var totalCount = filtered.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var paged = filtered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                // Thống kê
                ViewBag.Categories           = paged;
                ViewBag.TotalCount           = totalCount;
                ViewBag.PageNumber           = pageNumber;
                ViewBag.PageSize             = pageSize;
                ViewBag.TotalPages           = totalPages;
                ViewBag.SearchTerm           = searchTerm;
                ViewBag.TotalCategories      = await _categoryRepo.CountAsync();
                ViewBag.TotalProducts        = allCategories.Sum(c => c.Products?.Count ?? 0);
                ViewBag.CategoriesWithoutProducts = await _categoryRepo.CountEmptyAsync();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách danh mục");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách danh mục.";
                return View();
            }
        }

        // ══════════════════════════════════════════════════════════════
        // DETAILS - Chi tiết danh mục
        // GET: /admin/category/details/5
        // ══════════════════════════════════════════════════════════════
        [HttpGet("details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var category = await _categoryRepo.GetByIdWithDetailsAsync(id);
                if (category == null)
                {
                    TempData["Error"] = "Không tìm thấy danh mục.";
                    return RedirectToAction(nameof(Index));
                }

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chi tiết danh mục Id: {Id}", id);
                TempData["Error"] = "Đã xảy ra lỗi khi tải chi tiết danh mục.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ══════════════════════════════════════════════════════════════
        // CREATE - Tạo mới danh mục
        // GET: /admin/category/create
        // ══════════════════════════════════════════════════════════════
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.ParentCategories = await _categoryRepo.GetParentCategoriesAsync();
                return View(new Category());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang tạo danh mục");
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang tạo danh mục.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /admin/category/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.ParentCategories = await _categoryRepo.GetParentCategoriesAsync();
                    return View(category);
                }

                var success = await _categoryRepo.AddAsync(category);
                if (success)
                {
                    TempData["Success"] = $"Tạo danh mục \"{category.Name}\" thành công!";
                    return RedirectToAction(nameof(Details), new { id = category.Id });
                }

                TempData["Error"] = "Không thể lưu danh mục. Vui lòng thử lại.";
                ViewBag.ParentCategories = await _categoryRepo.GetParentCategoriesAsync();
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo danh mục: {CategoryName}", category.Name);
                TempData["Error"] = "Đã xảy ra lỗi khi tạo danh mục.";
                ViewBag.ParentCategories = await _categoryRepo.GetParentCategoriesAsync();
                return View(category);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // EDIT - Chỉnh sửa danh mục
        // GET: /admin/category/edit/5
        // ══════════════════════════════════════════════════════════════
        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var category = await _categoryRepo.GetByIdAsync(id);
                if (category == null)
                {
                    TempData["Error"] = "Không tìm thấy danh mục.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.ParentCategories = await _categoryRepo.GetParentCategoriesAsync(excludeId: id);
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang chỉnh sửa danh mục Id: {Id}", id);
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /admin/category/edit/5
        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            try
            {
                if (id != category.Id)
                    return NotFound();

                if (!ModelState.IsValid)
                {
                    ViewBag.ParentCategories = await _categoryRepo.GetParentCategoriesAsync(excludeId: id);
                    return View(category);
                }

                // Ngăn vòng lặp: danh mục không được là cha của chính nó
                if (category.ParentCategoryId == category.Id)
                {
                    ModelState.AddModelError("ParentCategoryId", "Danh mục không thể là cha của chính nó.");
                    ViewBag.ParentCategories = await _categoryRepo.GetParentCategoriesAsync(excludeId: id);
                    return View(category);
                }

                var success = await _categoryRepo.UpdateAsync(category);
                if (success)
                {
                    TempData["Success"] = $"Cập nhật danh mục \"{category.Name}\" thành công!";
                    return RedirectToAction(nameof(Details), new { id = category.Id });
                }

                TempData["Error"] = "Không thể cập nhật danh mục. Vui lòng thử lại.";
                ViewBag.ParentCategories = await _categoryRepo.GetParentCategoriesAsync(excludeId: id);
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật danh mục Id: {Id}", id);
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật danh mục.";
                ViewBag.ParentCategories = await _categoryRepo.GetParentCategoriesAsync(excludeId: id);
                return View(category);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // DELETE - Xóa danh mục (AJAX)
        // POST: /admin/category/delete
        // Quy tắc: Không cho xóa nếu có Sản Phẩm hoặc Danh Mục Con liên kết
        // ══════════════════════════════════════════════════════════════
        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int categoryId)
        {
            try
            {
                var category = await _categoryRepo.GetByIdAsync(categoryId);
                if (category == null)
                    return Json(new { success = false, message = "Không tìm thấy danh mục." });

                // Kiểm tra sản phẩm liên kết (fix bug: dùng CategoryId thay vì Id)
                if (await _categoryRepo.HasProductsAsync(categoryId))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể xóa danh mục \"{category.Name}\" vì đang có sản phẩm liên kết. Vui lòng chuyển sản phẩm sang danh mục khác trước."
                    });
                }

                // Kiểm tra danh mục con
                if (await _categoryRepo.HasSubCategoriesAsync(categoryId))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể xóa danh mục \"{category.Name}\" vì đang có danh mục con. Vui lòng xóa các danh mục con trước."
                    });
                }

                var success = await _categoryRepo.DeleteAsync(categoryId);
                if (success)
                    return Json(new { success = true, message = $"Đã xóa danh mục \"{category.Name}\" thành công." });

                return Json(new { success = false, message = "Không thể xóa danh mục. Vui lòng thử lại." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa danh mục Id: {Id}", categoryId);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa danh mục." });
            }
        }

        // ══════════════════════════════════════════════════════════════
        // EXPORT - Xuất CSV
        // GET: /admin/category/export
        // ══════════════════════════════════════════════════════════════
        [HttpGet("export")]
        public async Task<IActionResult> Export(string searchTerm = "")
        {
            try
            {
                var categories = await _categoryRepo.GetAllWithDetailsAsync();
                var query = categories.Where(c => c.ParentCategoryId == null);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                    query = query.Where(c =>
                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (c.Description != null && c.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));

                var csv = "Mã,Tên Danh Mục,Mô Tả,Số Sản Phẩm,Số Danh Mục Con\n";
                foreach (var c in query.OrderBy(x => x.Name))
                {
                    var desc = c.Description?.Replace("\"", "\"\"") ?? "";
                    csv += $"{c.Id},\"{c.Name}\",\"{desc}\",{c.Products?.Count ?? 0},{c.SubCategories?.Count ?? 0}\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", $"categories_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất CSV danh mục");
                TempData["Error"] = "Đã xảy ra lỗi khi xuất dữ liệu.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

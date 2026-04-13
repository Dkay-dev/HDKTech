using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    [Route("admin/[controller]")]
    public class CategoryController : Controller
    {
        private readonly HDKTechContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(HDKTechContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Display all categories with search and filter
        /// GET: /admin/category
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index(string searchTerm = "", int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                IQueryable<Category> query = _context.Categories
                    .AsNoTracking()
                    .Include(c => c.Products)
                    .Include(c => c.ParentCategory)
                    .Include(c => c.SubCategories);

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(c => 
                        c.Name.Contains(searchTerm) ||
                        c.Description.Contains(searchTerm));
                }

                // Filter only parent categories (không có cha)
                query = query.Where(c => c.ParentCategoryId == null);

                var totalCount = await query.CountAsync();
                var categories = await query
                    .OrderBy(c => c.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.Categories = categories;
                ViewBag.TotalCount = totalCount;
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.SearchTerm = searchTerm;

                // Summary statistics
                var totalCategories = await _context.Categories.AsNoTracking().CountAsync();
                var totalProducts = await _context.Products.AsNoTracking().CountAsync();
                var categoriesWithoutProducts = await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.Products.Count == 0)
                    .CountAsync();

                ViewBag.TotalCategories = totalCategories;
                ViewBag.TotalProducts = totalProducts;
                ViewBag.CategoriesWithoutProducts = categoriesWithoutProducts;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                TempData["Error"] = "Lỗi khi tải danh sách danh mục";
                return View();
            }
        }

        /// <summary>
        /// Display category details with subcategories and products
        /// GET: /admin/category/details/5
        /// </summary>
        [HttpGet]
        [Route("details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                        .ThenInclude(p => p.Images)
                    .Include(c => c.Products)
                        .ThenInclude(p => p.Inventories)
                    .Include(c => c.ParentCategory)
                    .Include(c => c.SubCategories)
                        .ThenInclude(dc => dc.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    TempData["Error"] = "Không tìm thấy danh mục";
                    return RedirectToAction("Index");
                }

                ViewBag.Category = category;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category details");
                TempData["Error"] = "Lỗi khi tải chi tiết danh mục";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Create new category
        /// GET: /admin/category/create
        /// </summary>
        [HttpGet]
        [Route("create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var parentCategories = await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.ParentCategoryId == null)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                ViewBag.ParentCategories = parentCategories;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create page");
                TempData["Error"] = "Lỗi khi tải trang tạo danh mục";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Save new category
        /// POST: /admin/category/create
        /// </summary>
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create(Category category)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(category);
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Tạo danh mục '{category.Name}' thành công";
                return RedirectToAction("Details", new { id = category.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                TempData["Error"] = "Lỗi khi tạo danh mục";
                return View(category);
            }
        }

        /// <summary>
        /// Edit category
        /// GET: /admin/category/edit/5
        /// </summary>
        [HttpGet]
        [Route("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.ParentCategory)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    TempData["Error"] = "Không tìm thấy danh mục";
                    return RedirectToAction("Index");
                }

                var parentCategories = await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.ParentCategoryId == null && c.Id != id)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                ViewBag.ParentCategories = parentCategories;
                ViewBag.Category = category;
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page");
                TempData["Error"] = "Lỗi khi tải trang chỉnh sửa";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Update category
        /// POST: /admin/category/edit/5
        /// </summary>
        [HttpPost]
        [Route("edit/{id}")]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            try
            {
                if (id != category.Id)
                {
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    return View(category);
                }

                _context.Categories.Update(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Cập nhật danh mục '{category.Name}' thành công";
                return RedirectToAction("Details", new { id = category.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                TempData["Error"] = "Lỗi khi cập nhật danh mục";
                return View(category);
            }
        }

        /// <summary>
        /// Delete category
        /// POST: /admin/category/delete
        /// </summary>
        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> Delete(int categoryId)
        {
            try
            {
                var category = await _context.Categories.FindAsync(categoryId);
                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                // Check if category has products
                var productCount = await _context.Products
                    .CountAsync(p => p.Id == categoryId);

                if (productCount > 0)
                {
                    return Json(new { success = false, message = $"Không thể xóa danh mục có {productCount} sản phẩm" });
                }

                // Check if category has subcategories
                var subcategoryCount = await _context.Categories
                    .CountAsync(c => c.ParentCategoryId == categoryId);

                if (subcategoryCount > 0)
                {
                    return Json(new { success = false, message = $"Không thể xóa danh mục có {subcategoryCount} danh mục con" });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Xóa danh mục '{category.Name}' thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                return Json(new { success = false, message = "Lỗi khi xóa danh mục" });
            }
        }

        /// <summary>
        /// Export categories to CSV
        /// GET: /admin/category/export
        /// </summary>
        [HttpGet]
        [Route("export")]
        public async Task<IActionResult> Export(string searchTerm = "")
        {
            try
            {
                IQueryable<Category> query = _context.Categories
                    .AsNoTracking()
                    .Include(c => c.Products)
                    .Where(c => c.ParentCategoryId == null);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(c => 
                        c.Name.Contains(searchTerm) ||
                        c.Description.Contains(searchTerm));
                }

                var categories = await query.OrderBy(c => c.Name).ToListAsync();

                var csv = "Tên Danh Mục,Mô Tả,Số Sản Phẩm\n";

                foreach (var category in categories)
                {
                    var description = category.Description?.Replace("\"", "\"\"") ?? "";
                    var productCount = category.Products?.Count ?? 0;
                    csv += $"\"{category.Name}\",\"{description}\",{productCount}\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", $"categories_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting categories");
                TempData["Error"] = "Lỗi khi xuất dữ liệu";
                return RedirectToAction("Index");
            }
        }
    }
}


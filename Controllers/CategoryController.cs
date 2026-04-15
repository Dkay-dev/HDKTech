using HDKTech.Repositories;
using HDKTech.Repositories.Interfaces;
using HDKTech.Models;
using Microsoft.AspNetCore.Mvc;

namespace HDKTech.Controllers
{
    public class CategoryController : Controller
    {
        private readonly CategoryRepository _categoryRepo;
        private readonly IProductRepository _productRepo;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            CategoryRepository categoryRepo,
            IProductRepository productRepo,
            ILogger<CategoryController> logger)
        {
            _categoryRepo = categoryRepo;
            _productRepo = productRepo;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // INDEX — hiển thị sản phẩm theo danh mục
        // URL: /Category/Index/{id}?sortBy=price_asc&brandId=2&minPrice=5000000
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(
            int id,
            string sortBy = "featured",
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? brandId = null,
            string? cpuLine = null,
            string? vgaLine = null,
            string? ramType = null,
            int? status = null,
            int page = 1)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null) return RedirectToAction("Index", "Home");

            // Dùng FilterProductsAsync — logic đệ quy danh mục con đã nằm trong Repository
            var filter = new ProductFilterModel
            {
                CategoryId = id,
                BrandId = brandId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Status = status,
                SortBy = sortBy,
                CpuLine = cpuLine,
                VgaLine = vgaLine,
                RamType = ramType
            };

            var allProducts = await _productRepo.FilterProductsAsync(filter);

            // ── Phân trang ────────────────────────────────────────────────────
            const int pageSize = 16;
            var totalProducts = allProducts.Count;
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var paged = allProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ── Lấy dữ liệu cho dropdown filter ──────────────────────────────
            var brands = await _productRepo.GetUniqueBrandsByCategory(id);
            var cpuLines = await _productRepo.GetUniqueCpuLines();

            ViewBag.CategoryName = category.Name;
            ViewBag.CategoryId = id;
            ViewBag.Brands = brands;
            ViewBag.CpuLines = cpuLines;
            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalProducts = totalProducts;

            return View(paged);
        }
    }
}
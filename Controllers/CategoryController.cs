using HDKTech.Repositories;
using HDKTech.Repositories.Interfaces;
using HDKTech.Models;
using Microsoft.AspNetCore.Mvc;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Controllers
{
    public class CategoryController : Controller
    {
        private readonly CategoryRepository _categoryRepo;
        private readonly IProductRepository _productRepo;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(CategoryRepository categoryRepo, IProductRepository productRepo, ILogger<CategoryController> logger)
        {
            _categoryRepo = categoryRepo;
            _productRepo = productRepo;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int id, string sortBy = "featured", decimal? minPrice = null, decimal? maxPrice = null, int? brandId = null, string? cpuLine = null, string? vgaLine = null, string? ramType = null, int? status = null, int page = 1)
        {
            var category = await _categoryRepo.GetByIdAsync(id);

            if (category == null) return RedirectToAction("Index", "Home");

            // Tìm danh mục root (cha cuối cùng)
            int rootCategoryId = id;
            var currentCategory = category;

            while (currentCategory.ParentCategoryId.HasValue && currentCategory.ParentCategoryId > 0)
            {
                var parent = await _categoryRepo.GetByIdAsync(currentCategory.ParentCategoryId.Value);
                if (parent != null)
                {
                    rootCategoryId = parent.Id;
                    currentCategory = parent;
                }
                else
                {
                    break;
                }
            }

            // Lấy sản phẩm từ danh mục root
            var products = await _categoryRepo.GetProductsByCategoryAsync(rootCategoryId);

            // ===== APPLY FILTERS =====
            // Nếu user click vào danh mục con (filter category), áp dụng filter dựa vào loại danh mục
            if (id != rootCategoryId)
            {
                var filterCategory = category;
                var parentCategory = filterCategory.ParentCategoryId.HasValue 
                    ? await _categoryRepo.GetByIdAsync(filterCategory.ParentCategoryId.Value) 
                    : null;

                // Map từ parent category ID để xác định loại filter
                // ID 15 = Thương hiệu (Brand)
                if (parentCategory?.Id == 15)
                {
                    var brandName = filterCategory.Name.ToLower();
                    products = products.Where(p => 
                        p.Brand?.Name?.ToLower().Contains(brandName) ?? false
                    ).ToList();
                }
                // ID 21 = Giá bán (Price)
                else if (parentCategory?.Id == 21)
                {
                    var priceRangeName = filterCategory.Name.ToLower();

                    if (priceRangeName.Contains("dưới") || priceRangeName.Contains("under"))
                    {
                        if (priceRangeName.Contains("15")) products = products.Where(p => p.Price < 15000000).ToList();
                    }
                    else if (priceRangeName.Contains("15") && priceRangeName.Contains("20"))
                    {
                        products = products.Where(p => p.Price >= 15000000 && p.Price <= 20000000).ToList();
                    }
                    else if (priceRangeName.Contains("trên") || priceRangeName.Contains("above"))
                    {
                        if (priceRangeName.Contains("20")) products = products.Where(p => p.Price > 20000000).ToList();
                    }
                }
                // ID 25 = CPU Intel
                else if (parentCategory?.Id == 25)
                {
                    var cpuName = filterCategory.Name;
                    products = products.Where(p => 
                        p.Specifications != null && p.Specifications.Contains(cpuName)
                    ).ToList();
                }
                // ID 26 = VGA
                else if (parentCategory?.Id == 26)
                {
                    var vgaName = filterCategory.Name;
                    products = products.Where(p => 
                        p.Specifications != null && p.Specifications.Contains(vgaName)
                    ).ToList();
                }
                // ID 27 = RAM
                else if (parentCategory?.Id == 27)
                {
                    var ramName = filterCategory.Name;
                    products = products.Where(p => 
                        p.Specifications != null && p.Specifications.Contains(ramName)
                    ).ToList();
                }
            }

            // Filter từ dropdown/query params
            if (brandId.HasValue && brandId > 0)
                products = products.Where(p => p.Id == brandId.Value).ToList();

            if (minPrice.HasValue)
                products = products.Where(p => p.Price >= minPrice.Value).ToList();
            if (maxPrice.HasValue)
                products = products.Where(p => p.Price <= maxPrice.Value).ToList();

            if (!string.IsNullOrWhiteSpace(cpuLine))
                products = products.Where(p => p.Specifications != null && p.Specifications.Contains(cpuLine)).ToList();

            if (!string.IsNullOrWhiteSpace(vgaLine))
                products = products.Where(p => p.Specifications != null && p.Specifications.Contains(vgaLine)).ToList();

            if (!string.IsNullOrWhiteSpace(ramType))
                products = products.Where(p => p.Specifications != null && p.Specifications.Contains(ramType)).ToList();

            if (status.HasValue)
                products = products.Where(p => p.Status == status.Value).ToList();

            // Áp dụng sắp xếp
            products = ApplySorting(products, sortBy);

            // ===== PAGINATION =====
            const int pageSize = 8;
            int totalProducts = products.Count;
            int totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var paginatedProducts = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

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

            return View(paginatedProducts);
        }

        private List<Product> ApplySorting(List<Product> products, string sortBy)
        {
            return sortBy?.ToLower() switch
            {
                "name_asc" => products.OrderBy(p => p.Name).ToList(),
                "name_desc" => products.OrderByDescending(p => p.Name).ToList(),
                "price_asc" => products.OrderBy(p => p.Price).ToList(),
                "price_desc" => products.OrderByDescending(p => p.Price).ToList(),
                "new" => products.OrderByDescending(p => p.CreatedAt).ToList(),
                _ => products.OrderByDescending(p => p.CreatedAt).ToList()
            };
        }
    }
}



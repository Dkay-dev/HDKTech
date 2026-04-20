// Controllers/CategoryController.cs — refactored
using HDKTech.Models;
using HDKTech.Repositories;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Services.Interfaces;

namespace HDKTech.Controllers
{
    public class CategoryController : Controller
    {
        private readonly CategoryRepository _categoryRepo;
        private readonly IProductService _productService;

        public CategoryController(
            CategoryRepository categoryRepo,
            IProductService productService)
        {
            _categoryRepo = categoryRepo;
            _productService = productService;
        }

        public async Task<IActionResult> Index(
            int id,
            string sortBy = "featured",
            string? brandIds = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? cpuFilter = null,
            string? vgaFilter = null,
            string? ramFilter = null,
            int page = 1)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null) return RedirectToAction("Index", "Home");

            var parsedBrandIds = ParseIntList(brandIds);

            var filter = new ProductFilterModel
            {
                CategoryId = id,
                BrandIds = parsedBrandIds,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                CpuFilter = cpuFilter,
                VgaFilter = vgaFilter,
                RamFilter = ramFilter,
                Page = page,
                PageSize = 16
            };

            var result = await _productService.FilterAsync(filter);

            ViewBag.CategoryName = category.Name;
            ViewBag.CategoryId = id;
            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.TotalProducts = result.TotalCount;
            ViewBag.SelectedBrandIds = parsedBrandIds;
            ViewBag.FilterOptions = result.Options; // Dynamic!
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(result.Products);
        }

        private static List<int> ParseIntList(string? raw)
            => (raw ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                           .Where(id => id > 0).Distinct().ToList();
    }
}
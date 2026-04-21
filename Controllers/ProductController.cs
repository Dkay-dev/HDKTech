using HDKTech.Models;
using HDKTech.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HDKTech.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _productService.GetDetailAsync(id.Value);
            if (product == null) return NotFound();

            ViewBag.RelatedProducts = await _productService.GetRelatedAsync(
                productId: product.Id,
                categoryId: product.CategoryId,
                limit: 8);

            return View(product);
        }

        public async Task<IActionResult> Filter(
            int? categoryId = null,
            string? brandIds = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? status = null,
            string sortBy = "featured",
            string? cpuFilter = null,
            string? vgaFilter = null,
            string? ramFilter = null,
            string? keyword = null,
            int page = 1)
        {
            var parsedBrandIds = ParseIntList(brandIds);

            var filter = new ProductFilterModel
            {
                CategoryId    = categoryId,
                BrandIds      = parsedBrandIds,
                MinPrice      = minPrice,
                MaxPrice      = maxPrice,
                Status        = status,
                SortBy        = sortBy,
                CpuFilter     = cpuFilter,
                VgaFilter     = vgaFilter,
                RamFilter     = ramFilter,
                SearchKeyword = keyword,
                Page          = page,
                PageSize      = 16
            };

            var result = await _productService.FilterAsync(filter);

            ViewBag.CurrentSort      = sortBy;
            ViewBag.CurrentPage      = page;
            ViewBag.TotalPages       = result.TotalPages;
            ViewBag.TotalProducts    = result.TotalCount;
            ViewBag.CategoryId       = categoryId;
            ViewBag.SelectedBrandIds = parsedBrandIds;
            ViewBag.BrandIds         = brandIds ?? "";
            ViewBag.MinPrice         = minPrice;
            ViewBag.MaxPrice         = maxPrice;
            ViewBag.CpuFilter        = cpuFilter;
            ViewBag.VgaFilter        = vgaFilter;
            ViewBag.RamFilter        = ramFilter;
            ViewBag.Keyword          = keyword;
            ViewBag.FilterOptions    = result.Options;

            return View(result.Products);
        }

        public async Task<IActionResult> Search(
            string keyword,
            string sortBy = "featured",
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? brandIds = null)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return RedirectToAction("Index", "Home");

            var parsedBrandIds = ParseIntList(brandIds);
            var filter = new ProductFilterModel
            {
                SearchKeyword = keyword,
                SortBy        = sortBy,
                MinPrice      = minPrice,
                MaxPrice      = maxPrice,
                BrandIds      = parsedBrandIds,
                Page          = 1,
                PageSize      = 48
            };

            var result = await _productService.FilterAsync(filter);

            ViewBag.Keyword          = keyword;
            ViewBag.ResultCount      = result.TotalCount;
            ViewBag.FilterOptions    = result.Options;
            ViewBag.CurrentSort      = sortBy;
            ViewBag.SelectedBrandIds = parsedBrandIds;
            ViewBag.MinPrice         = minPrice;
            ViewBag.MaxPrice         = maxPrice;

            return View(result.Products);
        }

        public async Task<IActionResult> FlashSale()
        {
            var products = await _productService.GetFlashSaleAsync(limit: 50);
            var endTime  = await _productService.GetFlashSaleEndTimeAsync();

            ViewBag.FlashSaleEndTime = (endTime ?? DateTime.Now.Date.AddDays(1)).ToString("o");
            ViewBag.TotalProducts    = products.Count;
            return View(products);
        }

        private static List<int> ParseIntList(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<int>();
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                      .Where(id => id > 0)
                      .Distinct()
                      .ToList();
        }
    }
}

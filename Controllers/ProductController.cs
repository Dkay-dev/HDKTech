// Controllers/ProductController.cs — refactored
using HDKTech.Models;
using HDKTech.Services;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Data;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly HDKTechContext _context; // chỉ dùng cho Brand lookup

        public ProductController(IProductService productService, HDKTechContext context)
        {
            _productService = productService;
            _context = context;
        }

        // ── DETAILS ──────────────────────────────────────────────────────
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

        // ── FILTER ───────────────────────────────────────────────────────
        public async Task<IActionResult> Filter(
            int? categoryId = null,
            string? brandIds = null,   // "1,2,3" → List<int>
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
                CategoryId = categoryId,
                BrandIds = parsedBrandIds,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Status = status,
                SortBy = sortBy,
                CpuFilter = cpuFilter,
                VgaFilter = vgaFilter,
                RamFilter = ramFilter,
                SearchKeyword = keyword,
                Page = page,
                PageSize = 16
            };

            var result = await _productService.FilterAsync(filter);

            // ViewBag cho View
            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.TotalProducts = result.TotalCount;
            ViewBag.CategoryId = categoryId;
            ViewBag.SelectedBrandIds = parsedBrandIds;
            ViewBag.BrandIds = brandIds ?? "";
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CpuFilter = cpuFilter;
            ViewBag.VgaFilter = vgaFilter;
            ViewBag.RamFilter = ramFilter;
            ViewBag.Keyword = keyword;

            // Filter options động — từ Service
            ViewBag.FilterOptions = result.Options;

            return View(result.Products);
        }

        // ── SEARCH ───────────────────────────────────────────────────────
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
                SortBy = sortBy,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                BrandIds = parsedBrandIds,
                Page = 1,
                PageSize = 48
            };

            var result = await _productService.FilterAsync(filter);

            ViewBag.Keyword = keyword;
            ViewBag.ResultCount = result.TotalCount;
            ViewBag.FilterOptions = result.Options;
            ViewBag.CurrentSort = sortBy;
            ViewBag.SelectedBrandIds = parsedBrandIds;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(result.Products);
        }

        // ── FLASH SALE — end time đọc từ bảng Promotion ──────────────────
        public async Task<IActionResult> FlashSale()
        {
            var products = await _productService.GetFlashSaleAsync(limit: 50);

            var now = DateTime.Now;
            var endTime = await _context.Promotions
                .Where(p => p.PromotionType == HDKTech.Areas.Admin.Models.PromotionType.FlashSale
                         && p.IsActive
                         && p.StartDate <= now && p.EndDate >= now)
                .Select(p => (DateTime?)p.EndDate)
                .DefaultIfEmpty(now.Date.AddDays(1))
                .MinAsync();

            ViewBag.FlashSaleEndTime = (endTime ?? now.Date.AddDays(1)).ToString("o");
            ViewBag.TotalProducts    = products.Count;
            return View(products);
        }

        // ── Helper ───────────────────────────────────────────────────────
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
using HDKTech.Repositories.Interfaces;
using HDKTech.Models;
using Microsoft.AspNetCore.Mvc;

namespace HDKTech.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo;

        public ProductController(IProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        // ─────────────────────────────────────────────────────────────────────
        // DETAILS
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _productRepo.GetProductWithDetailsAsync(id.Value);
            if (product == null) return NotFound();

            ViewBag.RelatedProducts = await _productRepo.GetRelatedProductsAsync(
                currentProductId: product.Id,
                categoryId: product.CategoryId,
                limit: 8);

            return View(product);
        }

        // ─────────────────────────────────────────────────────────────────────
        // SEARCH
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Search(
            string keyword,
            string sortBy = "featured",
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? brandNames = null)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return RedirectToAction("Index", "Home");

            var parsedBrands = ParseBrandNames(brandNames);

            var filter = new ProductFilterModel
            {
                SearchKeyword = keyword,
                SortBy = sortBy,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                BrandNames = parsedBrands
            };

            var products = await _productRepo.FilterProductsAsync(filter);
            var brands = await _productRepo.GetUniqueBrandsByCategory(0);

            ViewBag.Keyword = keyword;
            ViewBag.ResultCount = products.Count;
            ViewBag.Brands = brands;
            ViewBag.CurrentSort = sortBy;
            ViewBag.SelectedBrandNames = parsedBrands;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.BrandNames = brandNames ?? "";

            return View(products);
        }

        // ─────────────────────────────────────────────────────────────────────
        // FILTER  — receives brandNames as comma-separated brand name string
        //
        // URL example:
        //   /Product/Filter?categoryId=5&brandNames=Dell,ASUS&minPrice=10000000
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Filter(
            int? categoryId = null,
            string? brandNames = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? status = null,
            string sortBy = "featured",
            string? cpuLine = null,
            string? vgaLine = null,
            string? ramType = null,
            string? keyword = null,
            int page = 1)
        {
            var parsedBrands = ParseBrandNames(brandNames);

            var filter = new ProductFilterModel
            {
                CategoryId = categoryId,
                BrandNames = parsedBrands,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Status = status,
                SortBy = sortBy,
                CpuLine = cpuLine,
                VgaLine = vgaLine,
                RamType = ramType,
                SearchKeyword = keyword
            };

            var allProducts = await _productRepo.FilterProductsAsync(filter);

            // ── Pagination ────────────────────────────────────────────────────
            const int pageSize = 16;
            int totalProducts = allProducts.Count;
            int totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var paged = allProducts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // ── Brands available under this category (recursive) ──────────────
            var availableBrands = await _productRepo.GetUniqueBrandsByCategory(categoryId ?? 0);
            var cpuLines = await _productRepo.GetUniqueCpuLines();

            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.CategoryId = categoryId;
            ViewBag.BrandNames = brandNames ?? "";
            ViewBag.SelectedBrandNames = parsedBrands;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CpuLine = cpuLine;
            ViewBag.VgaLine = vgaLine;
            ViewBag.RamType = ramType;
            ViewBag.Keyword = keyword;
            ViewBag.Brands = availableBrands;
            ViewBag.CpuLines = cpuLines;

            return View(paged);
        }

        // ─────────────────────────────────────────────────────────────────────
        // FLASH SALE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> FlashSale()
        {
            var products = await _productRepo.GetFlashSaleProductsAsync(limit: 50);

            var endTime = products
                .Where(p => p.FlashSaleEndTime.HasValue)
                .Select(p => p.FlashSaleEndTime!.Value)
                .DefaultIfEmpty(DateTime.Now.Date.AddDays(1))
                .Min();

            ViewBag.FlashSaleEndTime = endTime.ToString("o");
            ViewBag.TotalProducts = products.Count;
            return View(products);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: "Dell,ASUS,Lenovo" → List<string>
        // ─────────────────────────────────────────────────────────────────────
        private static List<string> ParseBrandNames(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => !string.IsNullOrEmpty(s))
                      .Distinct()
                      .ToList();
        }
    }
}
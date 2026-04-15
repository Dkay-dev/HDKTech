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
        // DETAILS — trang chi tiết sản phẩm
        // FIX: Đảo tham số GetRelatedProductsAsync cho khớp interface mới
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _productRepo.GetProductWithDetailsAsync(id.Value);
            if (product == null) return NotFound();

            // FIX: currentProductId trước, categoryId sau
            ViewBag.RelatedProducts = await _productRepo.GetRelatedProductsAsync(
                currentProductId: product.Id,
                categoryId: product.CategoryId,
                limit: 8);

            return View(product);
        }

        // ─────────────────────────────────────────────────────────────────────
        // SEARCH — tìm kiếm toàn bộ sản phẩm
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Search(
            string keyword,
            string sortBy = "featured",
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? brandId = null)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return RedirectToAction("Index", "Home");

            var filter = new ProductFilterModel
            {
                SearchKeyword = keyword,
                SortBy = sortBy,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                BrandId = brandId
            };

            var products = await _productRepo.FilterProductsAsync(filter);
            var brands = await _productRepo.GetUniqueBrandsByCategory(0);

            ViewBag.Keyword = keyword;
            ViewBag.ResultCount = products.Count;
            ViewBag.Brands = brands;
            ViewBag.CurrentFilters = filter;
            ViewBag.CurrentSort = sortBy;

            return View(products);
        }

        // ─────────────────────────────────────────────────────────────────────
        // FILTER — FIX 2: Endpoint chính xử lý tất cả tham số từ Sidebar
        //
        // URL ví dụ từ sidebar:
        //   /Product/Filter?categoryId=5&brandId=2&minPrice=10000000&sortBy=price_asc
        //   /Product/Filter?categoryId=15             ← lọc theo danh mục cha
        //
        // Kết quả trả về View dùng .pc-grid + _ProductCard.cshtml
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Filter(
            int? categoryId = null,
            int? brandId = null,
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
            var filter = new ProductFilterModel
            {
                CategoryId = categoryId,
                BrandId = brandId,
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

            // ── ViewBag cho View ──────────────────────────────────────────────
            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.CategoryId = categoryId;
            ViewBag.BrandId = brandId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CpuLine = cpuLine;
            ViewBag.VgaLine = vgaLine;
            ViewBag.RamType = ramType;
            ViewBag.Keyword = keyword;

            return View(paged);
        }

        // ─────────────────────────────────────────────────────────────────────
        // FLASH SALE — trang Flash Sale
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
    }
}
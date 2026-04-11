using System.Diagnostics;
using HDKTech.Models;
using HDKTech.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HDKTech.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProductRepository _productRepo;
        private readonly CategoryRepository _categoryRepo;
        private readonly BannerRepository _bannerRepo;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, ProductRepository productRepo, CategoryRepository categoryRepo, BannerRepository bannerRepo)
        {
            _logger = logger;
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _bannerRepo = bannerRepo;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepo.GetAllAsync();

            // 🆕 Lấy banners hoạt động
            var activeBanners = await _bannerRepo.GetActiveBannersAsync();

            // ✅ Sửa: Sử dụng specialized methods từ repository (tránh N+1 query)
            var flashSaleProducts = await _productRepo.GetFlashSaleProductsAsync(limit: 5);
            var topSellerProducts = await _productRepo.GetTopSellerProductsAsync(limit: 8);
            var newProducts = await _productRepo.GetNewProductsAsync(limit: 6);

            // Tất cả sản phẩm cho hero slider (nếu cần)
            var allProducts = await _productRepo.GetAllWithImagesAsync();

            // Tạo ViewModel chứa các section khác nhau
            var viewModel = new HomeIndexViewModel
            {
                FlashSaleProducts = flashSaleProducts,
                TopSellerProducts = topSellerProducts,
                NewProducts = newProducts,
                AllProducts = allProducts.ToList(),

                // Danh mục chính (lấy danh mục không có cha - root categories)
                Categories = categories
                    .Where(c => c.MaDanhMucCha == null)
                    .OrderBy(c => c.MaDanhMuc)
                    .ToList(),

                // 🆕 Banners by type
                MainBanners = activeBanners
                    .Where(b => b.LoaiBanner == "Main")
                    .OrderBy(b => b.ThuTuHienThi)
                    .ToList(),

                SideBanners = activeBanners
                    .Where(b => b.LoaiBanner == "Side")
                    .OrderBy(b => b.ThuTuHienThi)
                    .ToList(),

                BottomBanners = activeBanners
                    .Where(b => b.LoaiBanner == "Bottom")
                    .OrderBy(b => b.ThuTuHienThi)
                    .ToList()
            };

            return View(viewModel);
        }

        public IActionResult Privacy() => View();

        public async Task<IActionResult> Diagnostic()
        {
            var allCategories = await _categoryRepo.GetAllAsync();
            var allProducts = await _productRepo.GetAllWithImagesAsync();

            var categoriesWithCount = allCategories.Select(c => new
            {
                c.MaDanhMuc,
                c.TenDanhMuc,
                c.MaDanhMucCha,
                ProductCount = allProducts.Count(p => p.MaDanhMuc == c.MaDanhMuc)
            }).ToList();

            ViewBag.Categories = categoriesWithCount;
            ViewBag.Products = allProducts.Take(20).ToList();
            ViewBag.TotalCategories = allCategories.Count;
            ViewBag.TotalProducts = allProducts.Count;
            ViewBag.EmptyCategories = categoriesWithCount.Count(c => c.ProductCount == 0);

            return View("~/Views/Shared/Diagnostic.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult About() => View();

        public IActionResult Hotline() => View();
    }
}
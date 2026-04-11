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
            
            var danhSachSanPham = await _productRepo.GetAllWithImagesAsync();
            var categories = await _categoryRepo.GetAllAsync();

            // 🆕 Lấy banners hoạt động
            var activeBanners = await _bannerRepo.GetActiveBannersAsync();

            // Tạo ViewModel chứa các section khác nhau
            var viewModel = new HomeIndexViewModel
            {
                // Flash Sale: 5 sản phẩm có discount cao nhất
                FlashSaleProducts = danhSachSanPham
                    .Where(p => p.PhanTramGiamGia > 0)
                    .OrderByDescending(p => p.PhanTramGiamGia)
                    .Take(5)
                    .ToList(),

                // Laptop bán chạy: Lọc từ danh mục Laptop (MaDanhMuc = 1) hoặc theo logic khác
                TopSellerProducts = danhSachSanPham
                    .OrderByDescending(p => p.MaSanPham)
                    .Take(8)
                    .ToList(),

                // Linh kiện mới về: Sắp xếp theo thời gian tạo
                NewProducts = danhSachSanPham
                    .OrderByDescending(p => p.ThoiGianTaoSP)
                    .Take(6)
                    .ToList(),

                // Tất cả sản phẩm cho hero slider
                AllProducts = danhSachSanPham.ToList(),

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
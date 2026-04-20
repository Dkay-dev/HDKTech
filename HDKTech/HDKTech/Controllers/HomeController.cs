using System.Diagnostics;
using HDKTech.Areas.Admin.Models;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories;
using HDKTech.Areas.Admin.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProductRepository   _productRepo;
        private readonly CategoryRepository  _categoryRepo;
        private readonly BannerRepository    _bannerRepo;
        private readonly HDKTechContext      _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ILogger<HomeController> logger,
            ProductRepository productRepo,
            CategoryRepository categoryRepo,
            BannerRepository bannerRepo,
            HDKTechContext context)
        {
            _logger       = logger;
            _productRepo  = productRepo;
            _categoryRepo = categoryRepo;
            _bannerRepo   = bannerRepo;
            _context      = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories    = await _categoryRepo.GetAllAsync();
            var activeBanners = await _bannerRepo.GetActiveBannersAsync();

            var flashSaleProducts  = await _productRepo.GetFlashSaleProductsAsync(limit: 5);
            var topSellerProducts  = await _productRepo.GetTopSellerProductsAsync(limit: 8);
            var newProducts        = await _productRepo.GetNewProductsAsync(limit: 6);

            // FlashSaleEndTime giờ đọc từ Promotion đang chạy (PromotionType.FlashSale).
            // Sửa lại dòng 45 trong HomeController.cs thành đoạn này:
            var flashSaleEndTime = await _context.Promotions
                .Where(p => p.PromotionType == PromotionType.FlashSale
                            && p.IsActive
                            && p.EndDate > DateTime.Now)
                .OrderBy(p => p.EndDate) // Lấy cái sắp kết thúc nhất
                .Select(p => (DateTime?)p.EndDate)
                .FirstOrDefaultAsync();

            // Truyền vào ViewBag để View sử dụng
            ViewBag.FlashSaleEndTime = flashSaleEndTime;

            var allProducts = await _productRepo.GetAllWithImagesAsync();

            var viewModel = new HomeIndexViewModel
            {
                FlashSaleProducts = flashSaleProducts,
                TopSellerProducts = topSellerProducts,
                NewProducts       = newProducts,
                AllProducts       = allProducts.ToList(),

                Categories = categories
                    .Where(c => c.ParentCategoryId == null)
                    .OrderBy(c => c.Id)
                    .ToList(),

                MainBanners = activeBanners
                    .Where(b => b.BannerType == "Main")
                    .OrderBy(b => b.DisplayOrder)
                    .ToList(),

                SideBanners = activeBanners
                    .Where(b => b.BannerType == "Side")
                    .OrderBy(b => b.DisplayOrder)
                    .ToList(),

                BottomBanners = activeBanners
                    .Where(b => b.BannerType == "Bottom")
                    .OrderBy(b => b.DisplayOrder)
                    .ToList()
            };

            return View(viewModel);
        }

        public IActionResult Privacy() => View();

        public async Task<IActionResult> Diagnostic()
        {
            var allCategories = await _categoryRepo.GetAllAsync();
            var allProducts   = await _productRepo.GetAllWithImagesAsync();

            var categoriesWithCount = allCategories.Select(c => new
            {
                c.Id,
                c.Name,
                c.ParentCategoryId,
                ProductCount = allProducts.Count(p => p.CategoryId == c.Id)
            }).ToList();

            ViewBag.Categories      = categoriesWithCount;
            ViewBag.Products        = allProducts.Take(20).ToList();
            ViewBag.TotalCategories = allCategories.Count;
            ViewBag.TotalProducts   = allProducts.Count;
            ViewBag.EmptyCategories = categoriesWithCount.Count(c => c.ProductCount == 0);

            return View("~/Views/Shared/Diagnostic.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        public IActionResult About()   => View();
        public IActionResult Hotline() => View();
    }
}

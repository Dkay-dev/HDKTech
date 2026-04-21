using System.Diagnostics;
using HDKTech.Models;
using HDKTech.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HDKTech.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IHomeService homeService)
        {
            _logger      = logger;
            _homeService = homeService;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = await _homeService.GetHomePageDataAsync();
            ViewBag.FlashSaleEndTime   = viewModel.FlashSaleEndTime;
            ViewBag.FlashSaleStartTime = viewModel.FlashSaleStartTime;
            return View(viewModel);
        }

        public IActionResult Privacy() => View();

        public async Task<IActionResult> Diagnostic()
        {
            var (allCategories, allProducts) = await _homeService.GetDiagnosticDataAsync();
            var productList = allProducts.ToList();

            var categoriesWithCount = allCategories.Select(c => new
            {
                c.Id,
                c.Name,
                c.ParentCategoryId,
                ProductCount = productList.Count(p => p.CategoryId == c.Id)
            }).ToList();

            ViewBag.Categories      = categoriesWithCount;
            ViewBag.Products        = productList.Take(20).ToList();
            ViewBag.TotalCategories = allCategories.Count();
            ViewBag.TotalProducts   = productList.Count;
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

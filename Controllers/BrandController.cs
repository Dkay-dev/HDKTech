using HDKTech.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HDKTech.Controllers
{
    public class BrandController : Controller
    {
        private readonly IBrandService _brandService;

        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        public async Task<IActionResult> Index(string slug = "")
        {
            if (string.IsNullOrEmpty(slug)) return RedirectToAction("Index", "Home");

            var (brand, products) = await _brandService.GetBrandPageAsync(slug);
            if (brand == null) return RedirectToAction("Index", "Home");

            ViewBag.CategoryName = brand.Name;
            return View(products);
        }
    }
}

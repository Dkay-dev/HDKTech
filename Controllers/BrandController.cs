using HDKTech.Areas.Identity.Data;
using HDKTech.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Controllers
{
    public class BrandController : Controller
    {
        private readonly HDKTechContext _DbContext;

        public BrandController(HDKTechContext DbContext)
        {
            _DbContext = DbContext;
        }

        public async Task<IActionResult> Index(string slug = "")
        {
            if (string.IsNullOrEmpty(slug)) return RedirectToAction("Index", "Home");

            // Tìm danh mục
            var category = await _DbContext.Brands
                .FirstOrDefaultAsync(c => c.Name.Trim().ToLower() == slug.Trim().ToLower());

            if (category == null) return RedirectToAction("Index", "Home");

            // Lấy sản phẩm
            var products = await _DbContext.Products
                                            .Where(p => p.Id == category.Id)
                                            .Include(p => p.Images)
                                            .OrderByDescending(p => p.Id)
                                            .ToListAsync();

            ViewBag.CategoryName = category.Id;

            return View(products);
        }
    }
}



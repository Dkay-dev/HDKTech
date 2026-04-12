using HDKTech.Repositories.Interfaces;
using HDKTech.Models;
using Microsoft.AspNetCore.Mvc;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo; // Dùng Interface thay vì Context

        public ProductController(IProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Gọi qua Repo, code ngắn và sạch hơn hẳn
            var product = await _productRepo.GetProductWithDetailsAsync(id.Value);

            if (product == null) return NotFound();

            // Lấy sản phẩm liên quan cũng qua Repo
            ViewBag.RelatedProducts = await _productRepo.GetRelatedProductsAsync(product.Id, product.Id, 4);

            // Use the unified premium Details layout with Grid 7-5, responsive design, and full specs tabs
            return View(product);
        }

        public async Task<IActionResult> Search(string keyword, string sortBy = "featured", decimal? minPrice = null, decimal? maxPrice = null, int? brandId = null)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return RedirectToAction("Index", "Home");
            }

            var filter = new ProductFilterModel
            {
                SearchKeyword = keyword,
                SortBy = sortBy,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                BrandId = brandId
            };

            var products = await _productRepo.FilterProductsAsync(filter);
            var brands = await _productRepo.GetUniqueBrandsByCategory(0); // 0 = all products

            ViewBag.Keyword = keyword;
            ViewBag.ResultCount = products.Count;
            ViewBag.Brands = brands;
            ViewBag.CurrentFilters = filter;
            ViewBag.CurrentSort = sortBy;

            return View(products);
        }
    }
}



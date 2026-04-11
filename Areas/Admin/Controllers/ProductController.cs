using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using HDKTech.Data;
using HDKTech.Utils;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin Product Management Controller
    /// Handles all CRUD operations for products in the admin area
    /// Requires Authorization
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    [Route("admin/[controller]")]
    public class ProductController : Controller
    {
        private readonly IAdminProductRepository _productRepository;
        private readonly ILogger<ProductController> _logger;
        private readonly HDKTechContext _context;

        public ProductController(
            IAdminProductRepository productRepository,
            ILogger<ProductController> logger,
            HDKTechContext context)
        {
            _productRepository = productRepository;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Display list of all products with pagination
        /// GET: /admin/product
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var (products, totalCount) = await _productRepository.GetProductsPagedAsync(page, pageSize);
                
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.TotalCount = totalCount;

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products list");
                TempData["Error"] = "Lỗi khi tải danh sách sản phẩm";
                return View(new List<SanPham>());
            }
        }

        /// <summary>
        /// Display product details for editing
        /// GET: /admin/product/details/{id}
        /// </summary>
        [HttpGet]
        [Route("details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var product = await _productRepository.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Sản phẩm không tìm thấy";
                    return RedirectToAction(nameof(Index));
                }

                await LoadViewBagData();
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for ID: {ProductId}", id);
                TempData["Error"] = "Lỗi khi tải chi tiết sản phẩm";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Display create product form
        /// GET: /admin/product/create
        /// </summary>
        [HttpGet]
        [Route("create")]
        public async Task<IActionResult> Create()
        {
            await LoadViewBagData();
            return View("Details", new SanPham());
        }

        /// <summary>
        /// Handle product creation
        /// POST: /admin/product/create
        /// </summary>
        [HttpPost]
        [Route("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SanPham product)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadViewBagData();
                    return View("Details", product);
                }

                // Set timestamps
                product.ThoiGianTaoSP = DateTime.Now;

                var createdProduct = await _productRepository.CreateProductAsync(product);

                TempData["Success"] = $"Sản phẩm '{product.TenSanPham}' đã được tạo thành công";
                return RedirectToAction(nameof(Details), new { id = createdProduct.MaSanPham });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError(string.Empty, "Lỗi khi tạo sản phẩm");
                await LoadViewBagData();
                return View("Details", product);
            }
        }

        /// <summary>
        /// Handle product update
        /// POST: /admin/product/edit/{id}
        /// </summary>
        [HttpPost]
        [Route("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SanPham product)
        {
            try
            {
                if (id != product.MaSanPham)
                {
                    return BadRequest("Mismatch product ID");
                }

                if (!ModelState.IsValid)
                {
                    await LoadViewBagData();
                    return View(nameof(Details), product);
                }

                var success = await _productRepository.UpdateProductAsync(product);

                if (success)
                {
                    TempData["Success"] = "Sản phẩm đã được cập nhật thành công";
                }
                else
                {
                    TempData["Error"] = "Cập nhật sản phẩm thất bại";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product ID: {ProductId}", id);
                ModelState.AddModelError(string.Empty, "Lỗi khi cập nhật sản phẩm");
                await LoadViewBagData();
                TempData["Error"] = "Lỗi khi cập nhật sản phẩm";
                return View(nameof(Details), product);
            }
        }

        /// <summary>
        /// Delete product
        /// POST: /admin/product/delete/{id}
        /// </summary>
        [HttpPost]
        [Route("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _productRepository.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound("Sản phẩm không tìm thấy");
                }

                var success = await _productRepository.DeleteProductAsync(id);

                if (success)
                {
                    TempData["Success"] = $"Sản phẩm '{product.TenSanPham}' đã được xóa thành công";
                }
                else
                {
                    TempData["Error"] = "Xóa sản phẩm thất bại";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product ID: {ProductId}", id);
                TempData["Error"] = "Lỗi khi xóa sản phẩm";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Bulk delete products
        /// POST: /admin/product/bulk-delete
        /// </summary>
        [HttpPost]
        [Route("bulk-delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] int[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return BadRequest("No products selected");
                }

                var success = await _productRepository.DeleteProductsAsync(ids);
                
                if (success)
                {
                    return Json(new { success = true, message = $"{ids.Length} sản phẩm đã được xóa" });
                }
                else
                {
                    return Json(new { success = false, message = "Xóa sản phẩm thất bại" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting products");
                return Json(new { success = false, message = "Lỗi khi xóa sản phẩm" });
            }
        }

        /// <summary>
        /// Update product stock
        /// POST: /admin/product/update-stock/{id}
        /// </summary>
        [HttpPost]
        [Route("update-stock/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, int quantity)
        {
            try
            {
                if (quantity < 0)
                {
                    return BadRequest("Số lượng không hợp lệ");
                }

                var success = await _productRepository.UpdateProductStockAsync(id, quantity);
                
                if (success)
                {
                    return Json(new { success = true, message = "Cập nhật kho thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Cập nhật kho thất bại" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product ID: {ProductId}", id);
                return Json(new { success = false, message = "Lỗi khi cập nhật kho" });
            }
        }

        /// <summary>
        /// Update product price
        /// POST: /admin/product/update-price/{id}
        /// </summary>
        [HttpPost]
        [Route("update-price/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrice(int id, decimal price)
        {
            try
            {
                if (price < 0)
                {
                    return BadRequest("Giá không hợp lệ");
                }

                var success = await _productRepository.UpdateProductPriceAsync(id, price);
                
                if (success)
                {
                    return Json(new { success = true, message = "Cập nhật giá thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Cập nhật giá thất bại" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating price for product ID: {ProductId}", id);
                return Json(new { success = false, message = "Lỗi khi cập nhật giá" });
            }
        }

        /// <summary>
        /// Search products
        /// GET: /admin/product/search
        /// </summary>
        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> Search(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return RedirectToAction(nameof(Index));
                }

                var products = await _productRepository.SearchProductsAsync(searchTerm);
                
                ViewBag.SearchTerm = searchTerm;
                ViewBag.ResultCount = products.Count();

                return View(nameof(Index), products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with term: {SearchTerm}", searchTerm);
                TempData["Error"] = "Lỗi khi tìm kiếm sản phẩm";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Load dropdown data (Categories, Brands)
        /// </summary>
        private async Task LoadViewBagData()
        {
            var categories = await _context.DanhMucs
                .Where(c => c.MaDanhMucCha == null)
                .OrderBy(c => c.TenDanhMuc)
                .ToListAsync();

            var brands = await _context.HangSXs
                .OrderBy(b => b.TenHangSX)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Brands = brands;
        }
    }
}

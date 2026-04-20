using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Models;
using HDKTech.Services;
using HDKTech.Utilities;
using HDKTech.Areas.Admin.Services.Interfaces;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminArea")]
    [Route("admin/[controller]")]
    public class ProductController : Controller
    {
        private readonly IProductAdminService      _productAdminService;
        private readonly ILogger<ProductController> _logger;
        private readonly IWebHostEnvironment        _env;
        private readonly ISystemLogService          _logService;

        public ProductController(
            IProductAdminService      productAdminService,
            ILogger<ProductController> logger,
            IWebHostEnvironment        env,
            ISystemLogService          logService)
        {
            _productAdminService = productAdminService;
            _logger              = logger;
            _env                 = env;
            _logService          = logService;
        }

        // ──────────────────────────────────────────────────────────────
        // INDEX
        // ──────────────────────────────────────────────────────────────
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(
            string searchTerm = "",
            int? categoryId   = null,
            int? brandId      = null,
            int page          = 1,
            int pageSize      = 15)
        {
            try
            {
                var (products, totalCount, categories, brands) =
                    await _productAdminService.GetProductsPagedAsync(searchTerm, categoryId, brandId, page, pageSize);

                ViewBag.Categories  = categories;
                ViewBag.Brands      = brands;
                ViewBag.SearchTerm  = searchTerm;
                ViewBag.CategoryId  = categoryId;
                ViewBag.BrandId     = brandId;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize    = pageSize;
                ViewBag.TotalPages  = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.TotalCount  = totalCount;

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải danh sách sản phẩm");
                TempData["Error"] = "Lỗi khi tải danh sách sản phẩm.";
                return View(new List<Product>());
            }
        }

        // ──────────────────────────────────────────────────────────────
        // DETAILS
        // ──────────────────────────────────────────────────────────────
        [HttpGet("details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productAdminService.GetProductByIdAsync(id);
            if (product == null)
            {
                TempData["Error"] = "Sản phẩm không tìm thấy.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            ViewBag.Variants  = product.Variants?.OrderByDescending(v => v.IsDefault).ToList()
                                ?? new List<ProductVariant>();
            ViewBag.FlashSale = await _productAdminService.GetFlashSaleForProductAsync(id);

            return View(product);
        }

        // ──────────────────────────────────────────────────────────────
        // CREATE
        // ──────────────────────────────────────────────────────────────
        [HttpGet("create")]
        [Authorize(Policy = "Product.Create")]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new Product { Status = 1, CreatedAt = DateTime.Now });
        }

        [HttpPost("create")]
        [Authorize(Policy = "Product.Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product product,
            IList<IFormFile> images,
            [Bind(Prefix = "DefaultVariant")] ProductVariant DefaultVariant,
            int InitialStock   = 0,
            bool IsFlashSale   = false,
            decimal FlashSalePrice = 0,
            DateTime? FlashSaleStart = null,
            DateTime? FlashSaleEnd   = null)
        {
            ModelState.Remove(nameof(Product.Category));
            ModelState.Remove(nameof(Product.Brand));
            ModelState.Remove(nameof(Product.Images));
            ModelState.Remove(nameof(Product.Variants));
            ModelState.Remove(nameof(Product.Reviews));
            ModelState.Remove(nameof(Product.Tags));

            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("DefaultVariant")).ToList())
                ModelState.Remove(key);

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(product);
            }

            try
            {
                product.CreatedAt = DateTime.Now;
                var created = await _productAdminService.CreateProductAsync(product);

                if (images?.Count > 0)
                    await _productAdminService.SaveProductImagesAsync(created.Id, images, created.Category?.Name);

                if (DefaultVariant != null && DefaultVariant.Price > 0)
                    await _productAdminService.CreateDefaultVariantAsync(created.Id, DefaultVariant, InitialStock);

                await _productAdminService.SaveFlashSaleAsync(
                    created.Id, created.Name, IsFlashSale, FlashSalePrice, FlashSaleStart, FlashSaleEnd);

                await LoggingHelper.LogCreateAsync(
                    username   : User.Identity?.Name ?? "Admin",
                    module     : "Product",
                    entityName : created.Name,
                    entityId   : created.Id.ToString(),
                    newValue   : new { created.Name, created.CategoryId, created.BrandId, created.Status });

                TempData["Success"] = $"Tạo sản phẩm \"{created.Name}\" thành công!";
                return RedirectToAction(nameof(Details), new { id = created.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo sản phẩm");
                TempData["Error"] = "Lỗi khi tạo sản phẩm.";
                await LoadDropdowns();
                return View(product);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // EDIT
        // ──────────────────────────────────────────────────────────────
        [HttpPost("edit/{id:int}")]
        [Authorize(Policy = "Product.Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product product,
            IList<IFormFile> images,
            [Bind(Prefix = "DefaultVariant")] ProductVariant DefaultVariant,
            bool IsFlashSale       = false,
            decimal FlashSalePrice = 0,
            DateTime? FlashSaleStart = null,
            DateTime? FlashSaleEnd   = null)
        {
            if (id != product.Id) return BadRequest();

            ModelState.Remove(nameof(Product.Category));
            ModelState.Remove(nameof(Product.Brand));
            ModelState.Remove(nameof(Product.Images));
            ModelState.Remove(nameof(Product.Variants));
            ModelState.Remove(nameof(Product.Reviews));
            ModelState.Remove(nameof(Product.Tags));

            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("DefaultVariant")).ToList())
                ModelState.Remove(key);

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View("Details", product);
            }

            try
            {
                var existing = await _productAdminService.GetProductWithVariantsAsync(id);
                if (existing == null) return NotFound();

                existing.Name             = product.Name;
                existing.Slug             = product.Slug;
                existing.CategoryId       = product.CategoryId;
                existing.BrandId          = product.BrandId;
                existing.WarrantyPolicyId = product.WarrantyPolicyId;
                existing.Status           = product.Status;
                existing.Description      = product.Description;
                existing.Specifications   = product.Specifications;
                existing.UpdatedAt        = DateTime.Now;

                await _productAdminService.UpdateProductAsync(existing);

                if (DefaultVariant != null && DefaultVariant.Price > 0)
                    await _productAdminService.UpdateDefaultVariantAsync(existing, DefaultVariant);

                if (images?.Count > 0)
                {
                    var catName = (await _productAdminService.GetProductByIdAsync(product.Id))?.Category?.Name;
                    await _productAdminService.SaveProductImagesAsync(product.Id, images, catName);
                }

                await _productAdminService.SaveFlashSaleAsync(
                    id, existing.Name, IsFlashSale, FlashSalePrice, FlashSaleStart, FlashSaleEnd);

                await LoggingHelper.LogUpdateAsync(
                    username   : User.Identity?.Name ?? "Admin",
                    module     : "Product",
                    entityName : existing.Name,
                    entityId   : id.ToString(),
                    oldValue   : new { existing.Name, existing.CategoryId, existing.BrandId, existing.Status },
                    newValue   : new { product.Name, product.CategoryId, product.BrandId, product.Status });

                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật sản phẩm Id: {Id}", id);
                TempData["Error"] = "Lỗi khi cập nhật sản phẩm.";
                await LoadDropdowns();
                return View("Details", product);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // DELETE IMAGE (AJAX)
        // ──────────────────────────────────────────────────────────────
        [HttpPost("delete-image/{imageId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            try
            {
                var url = await _productAdminService.DeleteImageAsync(imageId);
                if (url == null) return Json(new { success = false, message = "Không tìm thấy ảnh." });

                DeletePhysicalFile(url);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa ảnh Id: {Id}", imageId);
                return Json(new { success = false, message = "Lỗi khi xóa ảnh." });
            }
        }

        [HttpPost("set-default-image/{imageId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultImage(int imageId)
        {
            try
            {
                var ok = await _productAdminService.SetDefaultImageAsync(imageId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi set ảnh mặc định Id: {Id}", imageId);
                return Json(new { success = false });
            }
        }

        // ──────────────────────────────────────────────────────────────
        // DELETE PRODUCT
        // ──────────────────────────────────────────────────────────────
        [HttpPost("delete/{id:int}")]
        [Authorize(Policy = "Product.Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _productAdminService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Sản phẩm không tìm thấy.";
                    return RedirectToAction(nameof(Index));
                }

                var (success, error, imageUrls) = await _productAdminService.DeleteProductAsync(
                    id, User.Identity?.Name ?? "Admin");

                if (imageUrls != null)
                    foreach (var url in imageUrls) DeletePhysicalFile(url);

                if (success)
                {
                    await LoggingHelper.LogDeleteAsync(
                        username   : User.Identity?.Name ?? "Admin",
                        module     : "Product",
                        entityName : product.Name,
                        entityId   : id.ToString(),
                        oldValue   : new { product.Name, product.CategoryId, product.BrandId });
                }

                TempData[success ? "Success" : "Error"] = success
                    ? $"Đã xóa sản phẩm \"{product.Name}\"."
                    : $"Xóa sản phẩm thất bại: {error}";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa sản phẩm Id: {Id}", id);
                TempData["Error"] = "Lỗi khi xóa sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ──────────────────────────────────────────────────────────────
        // UPDATE STOCK (AJAX)
        // ──────────────────────────────────────────────────────────────
        [HttpPost("update-stock/{variantId:int}")]
        [Authorize(Policy = "Inventory.Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int variantId, int quantity)
        {
            if (quantity < 0)
                return Json(new { success = false, message = "Số lượng không hợp lệ." });

            var ok = await _productAdminService.UpdateVariantStockAsync(variantId, quantity);
            return Json(new
            {
                success = ok,
                message = ok ? "Cập nhật kho thành công." : "Cập nhật kho thất bại."
            });
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────
        private async Task LoadDropdowns()
        {
            var (categories, brands, warranties) = await _productAdminService.GetDropdownsAsync();
            ViewBag.Categories      = categories;
            ViewBag.Brands          = brands;
            ViewBag.WarrantyPolicies = warranties;
        }

        private void DeletePhysicalFile(string? relUrl)
        {
            if (string.IsNullOrWhiteSpace(relUrl)) return;
            try
            {
                var path = Path.Combine(_env.WebRootPath,
                    relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể xóa file: {Url}", relUrl);
            }
        }
    }
}

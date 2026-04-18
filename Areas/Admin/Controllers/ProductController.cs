using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Models;
using HDKTech.Data;
using Microsoft.EntityFrameworkCore;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireManager")]
    [Route("admin/[controller]")]
    public class ProductController : Controller
    {
        private readonly IAdminProductRepository _productRepo;
        private readonly ILogger<ProductController> _logger;
        private readonly HDKTechContext _context;
        private readonly IWebHostEnvironment _env;

        private const string ImgFolder = "images/products";

        public ProductController(
            IAdminProductRepository productRepo,
            ILogger<ProductController> logger,
            HDKTechContext context,
            IWebHostEnvironment env)
        {
            _productRepo = productRepo;
            _logger      = logger;
            _context     = context;
            _env         = env;
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
                IQueryable<Product> query = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Variants).ThenInclude(v => v.Inventories);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                    query = query.Where(p =>
                        p.Name.Contains(searchTerm) ||
                        (p.Description != null && p.Description.Contains(searchTerm)));

                if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
                if (brandId.HasValue)    query = query.Where(p => p.BrandId    == brandId.Value);

                var totalCount = await query.CountAsync();
                var products   = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.Categories  = await _context.Categories.AsNoTracking()
                                        .Where(c => c.ParentCategoryId == null)
                                        .OrderBy(c => c.Name).ToListAsync();
                ViewBag.Brands      = await _context.Brands.AsNoTracking()
                                        .OrderBy(b => b.Name).ToListAsync();
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
        // DETAILS — xem + sửa (Variants hiển thị trong view)
        // ──────────────────────────────────────────────────────────────
        [HttpGet("details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepo.GetProductByIdAsync(id);
            if (product == null)
            {
                TempData["Error"] = "Sản phẩm không tìm thấy.";
                return RedirectToAction(nameof(Index));
            }
            await LoadDropdowns();
            ViewBag.Variants = product.Variants?.OrderByDescending(v => v.IsDefault).ToList()
                               ?? new List<ProductVariant>();
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
            int InitialStock = 0)
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
                var created = await _productRepo.CreateProductAsync(product);

                if (images?.Count > 0)
                    await SaveProductImages(created.Id, images, created.Category?.Name);

                if (DefaultVariant != null && DefaultVariant.Price > 0)
                {
                    var sku = string.IsNullOrWhiteSpace(DefaultVariant.Sku)
                        ? $"P{created.Id}-DEFAULT"
                        : DefaultVariant.Sku.Trim();

                    var variant = new ProductVariant
                    {
                        ProductId   = created.Id,
                        Sku         = sku,
                        VariantName = "Mặc định",
                        Price       = DefaultVariant.Price,
                        ListPrice   = DefaultVariant.ListPrice,
                        IsActive    = true,
                        IsDefault   = true,
                        CreatedAt   = DateTime.Now
                    };
                    _context.ProductVariants.Add(variant);
                    await _context.SaveChangesAsync();

                    if (InitialStock > 0)
                    {
                        _context.Inventories.Add(new Inventory
                        {
                            ProductId        = created.Id,
                            ProductVariantId = variant.Id,
                            Quantity         = InitialStock,
                            UpdatedAt        = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }
                }

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
            [Bind(Prefix = "DefaultVariant")] ProductVariant DefaultVariant)
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
                var existing = await _context.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == id);
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

                if (DefaultVariant != null && DefaultVariant.Price > 0)
                {
                    var defVariant = existing.Variants?.FirstOrDefault(v => v.IsDefault)
                                     ?? existing.Variants?.FirstOrDefault();

                    if (defVariant == null)
                    {
                        defVariant = new ProductVariant
                        {
                            ProductId   = existing.Id,
                            Sku         = string.IsNullOrWhiteSpace(DefaultVariant.Sku)
                                            ? $"P{existing.Id}-DEFAULT"
                                            : DefaultVariant.Sku.Trim(),
                            VariantName = "Mặc định",
                            Price       = DefaultVariant.Price,
                            ListPrice   = DefaultVariant.ListPrice,
                            IsActive    = true,
                            IsDefault   = true,
                            CreatedAt   = DateTime.Now
                        };
                        _context.ProductVariants.Add(defVariant);
                    }
                    else
                    {
                        defVariant.Price     = DefaultVariant.Price;
                        defVariant.ListPrice = DefaultVariant.ListPrice;
                        if (!string.IsNullOrWhiteSpace(DefaultVariant.Sku))
                            defVariant.Sku = DefaultVariant.Sku.Trim();
                        defVariant.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();

                if (images?.Count > 0)
                {
                    var cat = await _context.Categories.FindAsync(product.CategoryId);
                    await SaveProductImages(product.Id, images, cat?.Name);
                }

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
                var img = await _context.ProductImages.FindAsync(imageId);
                if (img == null) return Json(new { success = false, message = "Không tìm thấy ảnh." });

                DeletePhysicalFile(img.ImageUrl);

                _context.ProductImages.Remove(img);
                await _context.SaveChangesAsync();
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
                var img = await _context.ProductImages.FindAsync(imageId);
                if (img == null) return Json(new { success = false });

                var siblings = _context.ProductImages.Where(x => x.ProductId == img.ProductId);
                await siblings.ForEachAsync(x => x.IsDefault = false);
                img.IsDefault = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
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
                var product = await _productRepo.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Sản phẩm không tìm thấy.";
                    return RedirectToAction(nameof(Index));
                }

                var (success, error, imageUrls) = await _productRepo.DeleteProductAsync(id);

                if (imageUrls != null)
                    foreach (var imgUrl in imageUrls) DeletePhysicalFile(imgUrl);

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
        // UPDATE STOCK (AJAX) — theo variant
        // ──────────────────────────────────────────────────────────────
        [HttpPost("update-stock/{variantId:int}")]
        [Authorize(Policy = "Inventory.Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int variantId, int quantity)
        {
            if (quantity < 0)
                return Json(new { success = false, message = "Số lượng không hợp lệ." });

            var ok = await _productRepo.UpdateVariantStockAsync(variantId, quantity);
            return Json(new
            {
                success = ok,
                message = ok ? "Cập nhật kho thành công." : "Cập nhật kho thất bại."
            });
        }

        // ──────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ──────────────────────────────────────────────────────────────
        private async Task SaveProductImages(int productId, IList<IFormFile> files, string? categoryName = null)
        {
            var subFolder = productId.ToString();
            var uploadDir = Path.Combine(_env.WebRootPath, ImgFolder, subFolder);
            Directory.CreateDirectory(uploadDir);

            bool isFirst = !await _context.ProductImages.AnyAsync(x => x.ProductId == productId);

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(ext)) continue;

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var physPath = Path.Combine(uploadDir, fileName);
                var relUrl   = $"/{ImgFolder}/{subFolder}/{fileName}";

                await using var stream = new FileStream(physPath, FileMode.Create);
                await file.CopyToAsync(stream);

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    ImageUrl  = relUrl,
                    IsDefault = isFirst,
                    AltText   = Path.GetFileNameWithoutExtension(file.FileName),
                    CreatedAt = DateTime.Now
                });
                isFirst = false;
            }

            await _context.SaveChangesAsync();
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

        private async Task LoadDropdowns()
        {
            ViewBag.Categories = await _context.Categories
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.Brands = await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();
            ViewBag.WarrantyPolicies = await _context.WarrantyPolicies
                .OrderBy(w => w.Name)
                .ToListAsync();
        }
    }
}

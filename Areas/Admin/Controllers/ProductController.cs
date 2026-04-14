using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Models;
using HDKTech.Data;
using HDKTech.Utils;
using Microsoft.EntityFrameworkCore;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    [Route("admin/[controller]")]
    public class ProductController : Controller
    {
        private readonly IAdminProductRepository _productRepo;
        private readonly ILogger<ProductController> _logger;
        private readonly HDKTechContext _context;
        private readonly IWebHostEnvironment _env;

        // Thư mục lưu ảnh sản phẩm (tương đối trong wwwroot)
        private const string ImgFolder = "images/products";

        public ProductController(
            IAdminProductRepository productRepo,
            ILogger<ProductController> logger,
            HDKTechContext context,
            IWebHostEnvironment env)
        {
            _productRepo = productRepo;
            _logger = logger;
            _context = context;
            _env = env;
        }

        // ──────────────────────────────────────────────────────────────
        // INDEX — danh sách sản phẩm với search + filter + phân trang
        // GET: /admin/product
        // ──────────────────────────────────────────────────────────────
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(
            string searchTerm = "",
            int? categoryId = null,
            int? brandId = null,
            int page = 1,
            int pageSize = 15)
        {
            try
            {
                IQueryable<Product> query = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Inventories);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                    query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));

                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId.Value);

                if (brandId.HasValue)
                    query = query.Where(p => p.BrandId == brandId.Value);

                var totalCount = await query.CountAsync();
                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Dropdowns cho filter
                ViewBag.Categories   = await _context.Categories.AsNoTracking().Where(c => c.ParentCategoryId == null).OrderBy(c => c.Name).ToListAsync();
                ViewBag.Brands       = await _context.Brands.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
                ViewBag.SearchTerm   = searchTerm;
                ViewBag.CategoryId   = categoryId;
                ViewBag.BrandId      = brandId;
                ViewBag.CurrentPage  = page;
                ViewBag.PageSize     = pageSize;
                ViewBag.TotalPages   = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.TotalCount   = totalCount;

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
        // DETAILS — xem/chỉnh sửa sản phẩm
        // GET: /admin/product/details/{id}
        // ──────────────────────────────────────────────────────────────
        [HttpGet("details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepo.GetProductByIdAsync(id);
            if (product == null) { TempData["Error"] = "Sản phẩm không tìm thấy."; return RedirectToAction(nameof(Index)); }
            await LoadDropdowns();
            return View(product);
        }

        // ──────────────────────────────────────────────────────────────
        // CREATE — form tạo mới
        // GET: /admin/product/create
        // ──────────────────────────────────────────────────────────────
        [HttpGet("create")]
        [Authorize(Policy = "Product.Create")]   // ← GĐ4: Granular Security
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new Product { Status = 1, CreatedAt = DateTime.Now });
        }

        // POST: /admin/product/create
        [HttpPost("create")]
        [Authorize(Policy = "Product.Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IList<IFormFile> images, int stockQuantity = 0)
        {
            // 1. Loại bỏ Validation cho các bảng liên quan
            ModelState.Remove(nameof(Product.Category));
            ModelState.Remove(nameof(Product.Brand));
            ModelState.Remove(nameof(Product.Images));
            ModelState.Remove(nameof(Product.Inventories));
            ModelState.Remove(nameof(Product.OrderItems));
            ModelState.Remove(nameof(Product.Reviews));

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(product);
            }

            try
            {
                product.CreatedAt = DateTime.Now;

                // --- ĐẢM BẢO DỮ LIỆU FLASH SALE ĐƯỢC GÁN ---
                // Thông thường EF Core sẽ tự động map nếu tên trường ở View 
                // và Model khớp nhau. Nhưng để chắc chắn, bạn có thể check:
                if (!product.IsFlashSale)
                {
                    product.FlashSalePrice = null;
                    product.FlashSaleEndTime = null;
                }

                // 2. Lưu sản phẩm thông qua Repo hoặc Context
                var created = await _productRepo.CreateProductAsync(product);

                // 3. Lưu ảnh nếu có
                if (images?.Count > 0)
                    await SaveProductImages(created.Id, images, created.Category?.Name);

                // 4. Tạo tồn kho
                if (stockQuantity > 0)
                {
                    _context.Inventories.Add(new Inventory
                    {
                        ProductId = created.Id,
                        Quantity = stockQuantity,
                        UpdatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
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
        // EDIT — cập nhật sản phẩm (dùng chung view Details)
        // POST: /admin/product/edit/{id}
        // ──────────────────────────────────────────────────────────────
        [HttpPost("edit/{id:int}")]
        [Authorize(Policy = "Product.Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IList<IFormFile> images)
        {
            if (id != product.Id) return BadRequest();

            // Bỏ validation các bảng liên quan để tránh lỗi vặt
            ModelState.Remove(nameof(Product.Category));
            ModelState.Remove(nameof(Product.Brand));
            ModelState.Remove(nameof(Product.Images));
            ModelState.Remove(nameof(Product.Inventories));
            ModelState.Remove(nameof(Product.OrderItems));
            ModelState.Remove(nameof(Product.Reviews));

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View("Details", product);
            }

            try
            {
                // 1. Lấy bản gốc từ DB ra để Update (Cách này an toàn nhất)
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null) return NotFound();

                // 2. Cập nhật các thông tin cơ bản
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.ListPrice = product.ListPrice;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.BrandId = product.BrandId;
                existingProduct.Status = product.Status;
                existingProduct.Description = product.Description;
                existingProduct.Specifications = product.Specifications;
                existingProduct.WarrantyInfo = product.WarrantyInfo;

                // 3. QUAN TRỌNG: Cập nhật Flash Sale
                existingProduct.IsFlashSale = product.IsFlashSale;
                existingProduct.FlashSalePrice = product.FlashSalePrice;
                existingProduct.FlashSaleEndTime = product.FlashSaleEndTime;

                // 4. Lưu trực tiếp qua Context hoặc qua Repo (Nếu Repo của bạn gọi SaveChanges)
                _context.Update(existingProduct);
                await _context.SaveChangesAsync();

                // Xử lý ảnh mới nếu có
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
        // DELETE IMAGE — xóa ảnh đơn lẻ (AJAX)
        // POST: /admin/product/delete-image/{imageId}
        // ──────────────────────────────────────────────────────────────
        [HttpPost("delete-image/{imageId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            try
            {
                var img = await _context.ProductImages.FindAsync(imageId);
                if (img == null) return Json(new { success = false, message = "Không tìm thấy ảnh." });

                // Xóa file vật lý
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

        // ──────────────────────────────────────────────────────────────
        // SET DEFAULT IMAGE (AJAX)
        // POST: /admin/product/set-default-image/{imageId}
        // ──────────────────────────────────────────────────────────────
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
        // POST: /admin/product/delete/{id}
        // ──────────────────────────────────────────────────────────────
        [HttpPost("delete/{id:int}")]
        [Authorize(Policy = "Product.Delete")]   // ← GĐ4: Granular Security
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _productRepo.GetProductByIdAsync(id);
                if (product == null) { TempData["Error"] = "Sản phẩm không tìm thấy."; return RedirectToAction(nameof(Index)); }

                var (success, error, imageUrls) = await _productRepo.DeleteProductAsync(id);

                // Xóa ảnh vật lý sau khi xóa khỏi DB
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
        // UPDATE STOCK (AJAX)
        // POST: /admin/product/update-stock/{id}
        // ──────────────────────────────────────────────────────────────
        [HttpPost("update-stock/{id:int}")]
        [Authorize(Policy = "Inventory.Update")]  // ← GĐ4: Granular Security
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, int quantity)
        {
            if (quantity < 0) return Json(new { success = false, message = "Số lượng không hợp lệ." });
            var ok = await _productRepo.UpdateProductStockAsync(id, quantity);
            return Json(new { success = ok, message = ok ? "Cập nhật kho thành công." : "Cập nhật kho thất bại." });
        }

        // ──────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ──────────────────────────────────────────────────────────────

        /// <summary>Lưu danh sách ảnh upload vào wwwroot/images/products/[productId]/</summary>
        private async Task SaveProductImages(int productId, IList<IFormFile> files, string? categoryName = null)
        {
            // Tạo sub-folder theo productId để dễ quản lý
            var subFolder = productId.ToString();
            var uploadDir = Path.Combine(_env.WebRootPath, ImgFolder, subFolder);
            Directory.CreateDirectory(uploadDir);

            bool isFirst = !await _context.ProductImages.AnyAsync(x => x.ProductId == productId);

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                // Chỉ chấp nhận ảnh
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(ext)) continue;

                var fileName   = $"{Guid.NewGuid():N}{ext}";
                var physPath   = Path.Combine(uploadDir, fileName);
                var relUrl     = $"/{ImgFolder}/{subFolder}/{fileName}";

                await using var stream = new FileStream(physPath, FileMode.Create);
                await file.CopyToAsync(stream);

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    ImageUrl  = relUrl,
                    IsDefault = isFirst,   // ảnh đầu tiên là mặc định
                    AltText   = Path.GetFileNameWithoutExtension(file.FileName),
                    CreatedAt = DateTime.Now
                });

                isFirst = false;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>Xóa file ảnh vật lý khỏi wwwroot</summary>
        private void DeletePhysicalFile(string? relUrl)
        {
            if (string.IsNullOrWhiteSpace(relUrl)) return;
            try
            {
                var path = Path.Combine(_env.WebRootPath, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Không thể xóa file: {Url}", relUrl); }
        }

        /// <summary>Load dropdowns Brand + Category cho form</summary>
        private async Task LoadDropdowns()
        {
            ViewBag.Categories = await _context.Categories
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.Brands = await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();
        }
    }
}


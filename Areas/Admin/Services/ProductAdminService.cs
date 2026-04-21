using HDKTech.Areas.Admin.Models;
using HDKTech.Areas.Admin.Repositories;
using HDKTech.Areas.Admin.Services.Interfaces;
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Services
{
    public class ProductAdminService : IProductAdminService
    {
        private readonly IAdminProductRepository _productRepo;
        private readonly HDKTechContext          _context;
        private readonly IWebHostEnvironment     _env;
        private readonly ILogger<ProductAdminService> _logger;

        private const string ImgFolder = "images/products";

        public ProductAdminService(
            IAdminProductRepository productRepo,
            HDKTechContext context,
            IWebHostEnvironment env,
            ILogger<ProductAdminService> logger)
        {
            _productRepo = productRepo;
            _context     = context;
            _env         = env;
            _logger      = logger;
        }

        // ── Listing ──────────────────────────────────────────────────────
        public async Task<(List<Product> Products, int TotalCount, List<Category> Categories, List<Brand> Brands)>
            GetProductsPagedAsync(string searchTerm, int? categoryId, int? brandId, int page, int pageSize)
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

            var categories = await _context.Categories.AsNoTracking()
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.Name).ToListAsync();

            var brands = await _context.Brands.AsNoTracking()
                .OrderBy(b => b.Name).ToListAsync();

            return (products, totalCount, categories, brands);
        }

        // ── Dropdowns ────────────────────────────────────────────────────
        public async Task<(List<Category> Categories, List<Brand> Brands, List<WarrantyPolicy> WarrantyPolicies)>
            GetDropdownsAsync()
        {
            var categories = await _context.Categories
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var brands = await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();

            var warranties = await _context.WarrantyPolicies
                .OrderBy(w => w.Name)
                .ToListAsync();

            return (categories, brands, warranties);
        }

        // ── Flash Sale ────────────────────────────────────────────────────
        public async Task<Promotion?> GetFlashSaleForProductAsync(int productId)
            => await _context.Set<PromotionProduct>()
                .Include(pp => pp.Promotion)
                .Where(pp => pp.ProductId == productId
                          && pp.Promotion != null
                          && pp.Promotion.PromotionType == PromotionType.FlashSale
                          && pp.Promotion.IsActive
                          && !pp.IsExclusion)
                .Select(pp => pp.Promotion)
                .FirstOrDefaultAsync();

        public async Task SaveFlashSaleAsync(int productId, string productName, bool isFlashSale,
            decimal flashSalePrice, DateTime? flashSaleStart, DateTime? flashSaleEnd)
        {
            var existing = await _context.Set<PromotionProduct>()
                .Include(pp => pp.Promotion)
                .Where(pp => pp.ProductId == productId
                          && pp.Promotion != null
                          && pp.Promotion.PromotionType == PromotionType.FlashSale
                          && !pp.IsExclusion)
                .FirstOrDefaultAsync();

            if (!isFlashSale || flashSalePrice <= 0)
            {
                if (existing?.Promotion != null)
                {
                    existing.Promotion.IsActive  = false;
                    existing.Promotion.Status    = PromotionStatus.Ended;
                    existing.Promotion.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                return;
            }

            var start = flashSaleStart ?? DateTime.Now;
            var end   = flashSaleEnd   ?? DateTime.Now.AddDays(1);

            if (existing?.Promotion != null)
            {
                var promo       = existing.Promotion;
                promo.Value     = flashSalePrice;
                promo.StartDate = start;
                promo.EndDate   = end;
                promo.IsActive  = true;
                promo.UpdatedAt = DateTime.Now;
                promo.Status    = CalcStatus(start, end);
                await _context.SaveChangesAsync();
            }
            else
            {
                var promo = new Promotion
                {
                    CampaignName  = $"Flash Sale — {productName}",
                    PromotionType = PromotionType.FlashSale,
                    Value         = flashSalePrice,
                    StartDate     = start,
                    EndDate       = end,
                    IsActive      = true,
                    AppliesToAll  = false,
                    Status        = CalcStatus(start, end),
                    CreatedAt     = DateTime.Now
                };
                _context.Set<Promotion>().Add(promo);
                await _context.SaveChangesAsync();

                _context.Set<PromotionProduct>().Add(new PromotionProduct
                {
                    PromotionId = promo.Id,
                    ProductId   = productId,
                    ScopeType   = PromotionScopeType.Product,
                    IsExclusion = false
                });
                await _context.SaveChangesAsync();
            }
        }

        private static PromotionStatus CalcStatus(DateTime start, DateTime end)
        {
            var now = DateTime.Now;
            if (now < start) return PromotionStatus.Scheduled;
            if (now > end)   return PromotionStatus.Ended;
            return PromotionStatus.Running;
        }

        // ── Variant + Inventory ───────────────────────────────────────────
        public async Task<ProductVariant> CreateDefaultVariantAsync(int productId, ProductVariant variant, int initialStock)
        {
            var sku = string.IsNullOrWhiteSpace(variant.Sku)
                ? $"P{productId}-DEFAULT"
                : variant.Sku.Trim();

            var entity = new ProductVariant
            {
                ProductId   = productId,
                Sku         = sku,
                VariantName = "Mặc định",
                Price       = variant.Price,
                ListPrice   = variant.ListPrice,
                IsActive    = true,
                IsDefault   = true,
                CreatedAt   = DateTime.Now
            };
            _context.ProductVariants.Add(entity);
            await _context.SaveChangesAsync();

            if (initialStock > 0)
            {
                _context.Inventories.Add(new Inventory
                {
                    ProductId        = productId,
                    ProductVariantId = entity.Id,
                    Quantity         = initialStock,
                    UpdatedAt        = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            return entity;
        }

        public async Task UpdateDefaultVariantAsync(Product existingProduct, ProductVariant variant)
        {
            var defVariant = existingProduct.Variants?.FirstOrDefault(v => v.IsDefault)
                             ?? existingProduct.Variants?.FirstOrDefault();

            if (defVariant == null)
            {
                defVariant = new ProductVariant
                {
                    ProductId   = existingProduct.Id,
                    Sku         = string.IsNullOrWhiteSpace(variant.Sku)
                                    ? $"P{existingProduct.Id}-DEFAULT"
                                    : variant.Sku.Trim(),
                    VariantName = "Mặc định",
                    Price       = variant.Price,
                    ListPrice   = variant.ListPrice,
                    IsActive    = true,
                    IsDefault   = true,
                    CreatedAt   = DateTime.Now
                };
                _context.ProductVariants.Add(defVariant);
            }
            else
            {
                defVariant.Price     = variant.Price;
                defVariant.ListPrice = variant.ListPrice;
                if (!string.IsNullOrWhiteSpace(variant.Sku))
                    defVariant.Sku = variant.Sku.Trim();
                defVariant.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        // ── Images ────────────────────────────────────────────────────────
        public async Task SaveProductImagesAsync(int productId, IList<IFormFile> files, string? categoryName = null)
        {
            var uploadDir = Path.Combine(_env.WebRootPath, ImgFolder, productId.ToString());
            Directory.CreateDirectory(uploadDir);

            bool isFirst = !await _context.ProductImages.AnyAsync(x => x.ProductId == productId);

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(ext)) continue;

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var physPath = Path.Combine(uploadDir, fileName);
                var relUrl   = $"/{ImgFolder}/{productId}/{fileName}";

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

        public async Task<string?> DeleteImageAsync(int imageId)
        {
            var img = await _context.ProductImages.FindAsync(imageId);
            if (img == null) return null;

            var url = img.ImageUrl;
            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();
            return url;
        }

        public async Task<bool> SetDefaultImageAsync(int imageId)
        {
            var img = await _context.ProductImages.FindAsync(imageId);
            if (img == null) return false;

            var siblings = _context.ProductImages.Where(x => x.ProductId == img.ProductId);
            await siblings.ForEachAsync(x => x.IsDefault = false);
            img.IsDefault = true;
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Product CRUD (delegates to IAdminProductRepository) ───────────
        public Task<Product?> GetProductByIdAsync(int id)
            => _productRepo.GetProductByIdAsync(id);

        public async Task<Product?> GetProductWithVariantsAsync(int id)
            => await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

        public Task<Product> CreateProductAsync(Product product)
            => _productRepo.CreateProductAsync(product);

        public Task<bool> UpdateProductAsync(Product product)
            => _productRepo.UpdateProductAsync(product);

        public Task<(bool Success, string? Error, IList<string> ImageUrls)> DeleteProductAsync(int id, string deletedBy)
            => _productRepo.DeleteProductAsync(id, deletedBy);

        public Task<bool> UpdateVariantStockAsync(int variantId, int quantity)
            => _productRepo.UpdateVariantStockAsync(variantId, quantity);
    }
}

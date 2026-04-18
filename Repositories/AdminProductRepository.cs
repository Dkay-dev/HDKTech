using HDKTech.Models;
using HDKTech.Data;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    /// <summary>
    /// Bản Admin repo "alias" trong namespace HDKTech.Repositories — giữ để
    /// tương thích với Program.cs cũ (registration thứ 2). Toàn bộ logic
    /// giờ deferred tới namespace mới trong Areas/Admin/Repositories.
    /// </summary>
    public class AdminProductRepository : IAdminProductRepository
    {
        private readonly HDKTechContext _context;
        private readonly ILogger<AdminProductRepository> _logger;

        public AdminProductRepository(HDKTechContext context, ILogger<AdminProductRepository> logger)
        {
            _context = context;
            _logger  = logger;
        }

        // ── Read ────────────────────────────────────────────────────
        public async Task<IEnumerable<Product>> GetAllProductsAsync() =>
            await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task<Product?> GetProductByIdAsync(int id) =>
            await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants).ThenInclude(v => v.Inventories)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId) =>
            await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task<IEnumerable<Product>> GetProductsByBrandAsync(int brandId) =>
            await _context.Products
                .Where(p => p.BrandId == brandId)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task<(IEnumerable<Product> products, int totalCount)> GetProductsPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants);

            var totalCount = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (products, totalCount);
        }

        // ── Create ──────────────────────────────────────────────────
        public async Task<Product> CreateProductAsync(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        // ── Update ──────────────────────────────────────────────────
        public async Task<bool> UpdateProductAsync(Product product)
        {
            if (product == null) return false;

            var existing = await _context.Products.FindAsync(product.Id);
            if (existing == null) return false;

            existing.Name             = product.Name;
            existing.Slug             = product.Slug;
            existing.Description      = product.Description;
            existing.CategoryId       = product.CategoryId;
            existing.BrandId          = product.BrandId;
            existing.WarrantyPolicyId = product.WarrantyPolicyId;
            existing.Status           = product.Status;
            existing.Specifications   = product.Specifications;
            existing.UpdatedAt        = DateTime.Now;

            _context.Products.Update(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateVariantStockAsync(int productVariantId, int quantity)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductVariantId == productVariantId);

            if (inv == null)
            {
                var variant = await _context.ProductVariants.FindAsync(productVariantId);
                if (variant == null) return false;

                _context.Inventories.Add(new Inventory
                {
                    ProductVariantId = productVariantId,
                    ProductId        = variant.ProductId,
                    Quantity         = quantity,
                    UpdatedAt        = DateTime.Now
                });
            }
            else
            {
                inv.Quantity  = quantity;
                inv.UpdatedAt = DateTime.Now;
                _context.Inventories.Update(inv);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateVariantPriceAsync(int productVariantId, decimal price)
        {
            var variant = await _context.ProductVariants.FindAsync(productVariantId);
            if (variant == null) return false;

            variant.Price     = price;
            variant.UpdatedAt = DateTime.Now;
            _context.ProductVariants.Update(variant);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Delete ──────────────────────────────────────────────────
        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductsAsync(IEnumerable<int> ids)
        {
            var products = await _context.Products
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            if (products.Count == 0) return false;

            _context.Products.RemoveRange(products);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Search / Filter ─────────────────────────────────────────
        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return await GetAllProductsAsync();

            var kw = searchTerm.ToLower();
            return await _context.Products
                .Where(p =>
                    p.Name.ToLower().Contains(kw) ||
                    (p.Description != null && p.Description.ToLower().Contains(kw)))
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> FilterProductsAsync(ProductFilterCriteria criteria)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Variants).ThenInclude(v => v.Inventories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
            {
                var kw = criteria.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(kw) ||
                    (p.Description != null && p.Description.ToLower().Contains(kw)));
            }

            if (criteria.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == criteria.CategoryId.Value);
            if (criteria.BrandId.HasValue)
                query = query.Where(p => p.BrandId == criteria.BrandId.Value);

            if (criteria.MinPrice.HasValue)
                query = query.Where(p => p.Variants.Any(v => v.IsActive && v.Price >= criteria.MinPrice.Value));
            if (criteria.MaxPrice.HasValue)
                query = query.Where(p => p.Variants.Any(v => v.IsActive && v.Price <= criteria.MaxPrice.Value));

            if (criteria.InStock == true)
                query = query.Where(p =>
                    p.Variants.Any(v => v.Inventories.Any(i => i.Quantity - i.ReservedQuantity > 0)));

            if (criteria.IsActive == true)
                query = query.Where(p => p.Status == 1);

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        // ── Exists ──────────────────────────────────────────────────
        public async Task<bool> ProductExistsAsync(int id) =>
            await _context.Products.AnyAsync(p => p.Id == id);

        public async Task<bool> CheckSkuExistsAsync(string sku, int? excludeVariantId = null)
        {
            if (string.IsNullOrWhiteSpace(sku)) return false;
            var q = _context.ProductVariants.Where(v => v.Sku == sku);
            if (excludeVariantId.HasValue)
                q = q.Where(v => v.Id != excludeVariantId.Value);
            return await q.AnyAsync();
        }
    }
}

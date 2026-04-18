using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    /// <summary>
    /// ProductRepository — phiên bản refactor sau khi tách Price/Flash-sale
    /// khỏi Product. Tất cả truy vấn giá đều đi qua ProductVariants, và
    /// Flash Sale đi qua bảng Promotion.
    /// </summary>
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(HDKTechContext context) : base(context) { }

        // ─────────────────────────────────────────────────────────────────────
        // Related (cùng category)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<Product>> GetRelatedProductsAsync(
            int currentProductId, int categoryId, int limit)
        {
            return await _dbSet
                .Where(p => p.CategoryId == categoryId && p.Id != currentProductId)
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // FilterProductsAsync — lọc qua ProductVariants (giá) + Category tree
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<Product>> FilterProductsAsync(ProductFilterModel filter)
        {
            var query = _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsQueryable();

            // Category (recursive)
            if (filter.CategoryId.HasValue && filter.CategoryId > 0)
            {
                var allCategoryIds = await GetAllDescendantCategoryIds(filter.CategoryId.Value);
                query = query.Where(p => allCategoryIds.Contains(p.CategoryId));
            }

            // Brand (multi)
            if (filter.BrandIds != null && filter.BrandIds.Any())
                query = query.Where(p => filter.BrandIds.Contains(p.BrandId));
            else if (filter.BrandNames != null && filter.BrandNames.Any())
                query = query.Where(p => p.Brand != null && filter.BrandNames.Contains(p.Brand.Name));

            // Price range → lọc qua Variant
            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Variants.Any(v => v.IsActive && v.Price >= filter.MinPrice.Value));

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Variants.Any(v => v.IsActive && v.Price <= filter.MaxPrice.Value));

            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                var kw = filter.SearchKeyword.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(kw) ||
                    (p.Description != null && p.Description.ToLower().Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(filter.CpuFilter))
                query = query.Where(p => p.Variants.Any(v => v.Cpu != null && v.Cpu.Contains(filter.CpuFilter)));

            if (!string.IsNullOrWhiteSpace(filter.VgaFilter))
                query = query.Where(p => p.Variants.Any(v => v.Gpu != null && v.Gpu.Contains(filter.VgaFilter)));

            if (!string.IsNullOrWhiteSpace(filter.RamFilter))
                query = query.Where(p => p.Variants.Any(v => v.Ram != null && v.Ram.Contains(filter.RamFilter)));

            query = ApplySortBy(query, filter.SortBy);
            return await query.ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetUniqueBrandsByCategory — recursive
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<string>> GetUniqueBrandsByCategory(int categoryId)
        {
            var query = _dbSet.Include(p => p.Brand).AsQueryable();

            if (categoryId > 0)
            {
                var allIds = await GetAllDescendantCategoryIds(categoryId);
                query = query.Where(p => allIds.Contains(p.CategoryId));
            }

            return await query
                .Where(p => p.Brand != null)
                .Select(p => p.Brand!.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();
        }

        private async Task<List<int>> GetAllDescendantCategoryIds(int rootId)
        {
            var result = new List<int> { rootId };
            var queue = new Queue<int>();
            queue.Enqueue(rootId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var children = await _context.Categories
                    .Where(c => c.ParentCategoryId == current)
                    .Select(c => c.Id)
                    .ToListAsync();

                foreach (var child in children)
                {
                    if (!result.Contains(child))
                    {
                        result.Add(child);
                        queue.Enqueue(child);
                    }
                }
            }

            return result;
        }

        // Giá/order-by giá phải dựa trên variant mặc định.
        private static IQueryable<Product> ApplySortBy(IQueryable<Product> q, string? s) =>
            s?.ToLower() switch
            {
                "name_asc"   => q.OrderBy(p => p.Name),
                "name_desc"  => q.OrderByDescending(p => p.Name),
                "price_asc"  => q.OrderBy(p => p.Variants
                                    .Where(v => v.IsActive)
                                    .Select(v => (decimal?)v.Price)
                                    .Min() ?? 0m),
                "price_desc" => q.OrderByDescending(p => p.Variants
                                    .Where(v => v.IsActive)
                                    .Select(v => (decimal?)v.Price)
                                    .Max() ?? 0m),
                "new"        => q.OrderByDescending(p => p.CreatedAt),
                _            => q.OrderByDescending(p => p.CreatedAt)
            };

        public async Task<List<Product>> GetAllWithImagesAsync() =>
            await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .ToListAsync();

        public async Task<Product?> GetProductWithDetailsAsync(int id) =>
            await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Reviews).ThenInclude(r => r.User)
                .Include(p => p.Variants).ThenInclude(v => v.Inventories)
                .FirstOrDefaultAsync(m => m.Id == id);

        public async Task<List<string>> GetUniqueCpuLines()
        {
            return await _context.ProductVariants
                .Where(v => v.Cpu != null && v.Cpu != "")
                .Select(v => v.Cpu!)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Flash Sale — không còn cột trên Product, đọc từ Promotion.
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<Product>> GetFlashSaleProductsAsync(int limit = 5)
        {
            var now = DateTime.Now;

            // Lấy các product nằm trong khuyến mãi FlashSale đang chạy
            var productIds = await _context.PromotionProducts
                .Where(pp => pp.Promotion != null
                          && pp.Promotion.PromotionType == Areas.Admin.Models.PromotionType.FlashSale
                          && pp.Promotion.IsActive
                          && pp.Promotion.StartDate <= now
                          && pp.Promotion.EndDate >= now)
                .Select(pp => pp.ProductId ?? (pp.Variant != null ? pp.Variant.ProductId : (int?)null))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .Take(limit)
                .ToListAsync();

            if (!productIds.Any()) return new List<Product>();

            return await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();
        }

        public async Task<List<Product>> GetTopSellerProductsAsync(int limit = 8) =>
            await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.Id)
                .Take(limit)
                .ToListAsync();

        public async Task<List<Product>> GetNewProductsAsync(int limit = 6) =>
            await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
    }
}

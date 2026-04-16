using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(HDKTechContext context) : base(context) { }

        public async Task<List<Product>> GetRelatedProductsAsync(
            int currentProductId, int categoryId, int limit)
        {
            return await _dbSet
                .Where(p => p.CategoryId == categoryId && p.Id != currentProductId)
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // FilterProductsAsync — recursive category + brand-by-name
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<Product>> FilterProductsAsync(ProductFilterModel filter)
        {
            var query = _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .AsQueryable();

            // RECURSIVE category: includes all child/grandchild categories
            if (filter.CategoryId.HasValue && filter.CategoryId > 0)
            {
                var allCategoryIds = await GetAllDescendantCategoryIds(filter.CategoryId.Value);
                query = query.Where(p => allCategoryIds.Contains(p.CategoryId));
            }

            // Brand by NAME (multi-select). Using name avoids the GetHashCode bug.
            if (filter.BrandNames != null && filter.BrandNames.Any())
            {
                query = query.Where(p => p.Brand != null &&
                                         filter.BrandNames.Contains(p.Brand.Name));
            }

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                var kw = filter.SearchKeyword.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(kw) ||
                    (p.Description != null && p.Description.ToLower().Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(filter.CpuLine))
                query = query.Where(p => p.Specifications != null &&
                                         p.Specifications.Contains(filter.CpuLine));

            if (!string.IsNullOrWhiteSpace(filter.VgaLine))
                query = query.Where(p => p.Specifications != null &&
                                         p.Specifications.Contains(filter.VgaLine));

            if (!string.IsNullOrWhiteSpace(filter.RamType))
                query = query.Where(p => p.Specifications != null &&
                                         p.Specifications.Contains(filter.RamType));

            query = ApplySortBy(query, filter.SortBy);
            return await query.ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetUniqueBrandsByCategory — now RECURSIVE too
        // FIX: old version only fetched brands from exact categoryId, not children
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

        // BFS: root + all descendants
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

        private static IQueryable<Product> ApplySortBy(IQueryable<Product> q, string? s) =>
            s?.ToLower() switch
            {
                "name_asc" => q.OrderBy(p => p.Name),
                "name_desc" => q.OrderByDescending(p => p.Name),
                "price_asc" => q.OrderBy(p => p.Price),
                "price_desc" => q.OrderByDescending(p => p.Price),
                "new" => q.OrderByDescending(p => p.CreatedAt),
                _ => q.OrderByDescending(p => p.CreatedAt)
            };

        public async Task<List<Product>> GetAllWithImagesAsync() =>
            await _dbSet.Include(p => p.Images).Include(p => p.Brand)
                        .Include(p => p.Category).ToListAsync();

        public async Task<Product?> GetProductWithDetailsAsync(int id) =>
            await _dbSet
                .Include(p => p.Images).Include(p => p.Brand).Include(p => p.Category)
                .Include(p => p.Reviews).ThenInclude(r => r.User)
                .Include(p => p.Inventories)
                .FirstOrDefaultAsync(m => m.Id == id);

        public async Task<List<string>> GetUniqueCpuLines()
        {
            var specs = await _dbSet
                .Where(p => p.Specifications != null && p.Specifications != "")
                .Select(p => p.Specifications!).Distinct().ToListAsync();

            var set = new HashSet<string>();
            var patterns = new[] { "i3", "i5", "i7", "i9", "Ryzen 3", "Ryzen 5", "Ryzen 7", "Ryzen 9" };
            foreach (var spec in specs)
                foreach (var p in patterns)
                    if (spec?.Contains(p, StringComparison.OrdinalIgnoreCase) == true)
                        set.Add(p);

            return set.OrderBy(s => s).ToList();
        }

        public async Task<List<Product>> GetFlashSaleProductsAsync(int limit = 5)
        {
            var now = DateTime.Now;
            return await _dbSet
                .Include(p => p.Images).Include(p => p.Brand)
                .Include(p => p.Category).Include(p => p.Inventories)
                .Where(p => p.IsFlashSale && p.FlashSalePrice.HasValue &&
                            p.FlashSalePrice < p.Price &&
                            p.FlashSaleEndTime.HasValue && p.FlashSaleEndTime.Value > now)
                .OrderBy(p => p.FlashSaleEndTime).Take(limit).ToListAsync();
        }

        public async Task<List<Product>> GetTopSellerProductsAsync(int limit = 8) =>
            await _dbSet.Include(p => p.Images).Include(p => p.Brand)
                        .Include(p => p.Category).Include(p => p.Inventories)
                        .OrderByDescending(p => p.Id).Take(limit).ToListAsync();

        public async Task<List<Product>> GetNewProductsAsync(int limit = 6) =>
            await _dbSet.Include(p => p.Images).Include(p => p.Brand)
                        .Include(p => p.Category).Include(p => p.Inventories)
                        .OrderByDescending(p => p.CreatedAt).Take(limit).ToListAsync();
    }
}
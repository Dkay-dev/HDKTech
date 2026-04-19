// Services/ProductService.cs — refactor sau khi Price/FlashSale rời khỏi Product
using HDKTech.Areas.Admin.Models;
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Services
{
    public class ProductService : IProductService
    {
        private readonly HDKTechContext _context;
        private readonly ICategoryCacheService _categoryCache;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            HDKTechContext context,
            ICategoryCacheService categoryCache,
            ILogger<ProductService> logger)
        {
            _context = context;
            _categoryCache = categoryCache;
            _logger = logger;
        }

        // ── MAIN FILTER ────────────────────────────────────────────
        public async Task<ProductFilterResult> FilterAsync(ProductFilterModel filter)
        {
            var query = BuildBaseQuery(filter);

            var totalCount = await query.CountAsync();
            var products = await ApplySortAndPage(query, filter).ToListAsync();
            var options  = await ComputeFilterOptionsAsync(filter);

            return new ProductFilterResult
            {
                Products  = products,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
                Options   = options
            };
        }

        // ── DETAIL (kèm toàn bộ Variant + Inventory) ───────────────
        public async Task<Product?> GetDetailAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Reviews).ThenInclude(r => r.User)
                .Include(p => p.Variants).ThenInclude(v => v.Inventories)
                .Include(p => p.Tags)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // ── RELATED ────────────────────────────────────────────────
        public async Task<List<Product>> GetRelatedAsync(int productId, int categoryId, int limit = 8)
        {
            var categoryIds = _categoryCache.GetDescendantCategoryIds(categoryId);

            return await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Where(p => categoryIds.Contains(p.CategoryId) && p.Id != productId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync();
        }

        // ── FLASH SALE — đọc từ Promotion (PromotionType.FlashSale) ─
        public async Task<List<Product>> GetFlashSaleAsync(int limit = 5)
        {
            var now = DateTime.Now;

            var productIds = await _context.PromotionProducts
                .Where(pp => pp.Promotion != null
                          && pp.Promotion.PromotionType == PromotionType.FlashSale
                          && pp.Promotion.IsActive
                          && pp.Promotion.StartDate <= now
                          && pp.Promotion.EndDate   >= now)
                .Select(pp => pp.ProductId ?? (pp.Variant != null ? pp.Variant.ProductId : (int?)null))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .Take(limit)
                .ToListAsync();

            if (!productIds.Any()) return new List<Product>();

            return await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Where(p => productIds.Contains(p.Id))
                .AsNoTracking()
                .ToListAsync();
        }

        // ── Build base query ───────────────────────────────────────
        private IQueryable<Product> BuildBaseQuery(ProductFilterModel filter)
        {
            var query = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsNoTracking()
                .AsQueryable();

            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                var categoryIds = _categoryCache.GetDescendantCategoryIds(filter.CategoryId.Value);
                query = query.Where(p => categoryIds.Contains(p.CategoryId));
            }

            if (filter.BrandIds?.Any() == true)
                query = query.Where(p => p.BrandId.HasValue && filter.BrandIds.Contains(p.BrandId.Value));

            // Price range ⇒ qua Variant
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

            // Spec filters — có thể tra qua ProductTag hoặc Variant trực tiếp
            if (!string.IsNullOrWhiteSpace(filter.RamFilter))
                query = query.Where(p =>
                    p.Tags!.Any(t => t.TagKey == "RAM" && t.TagValue.Contains(filter.RamFilter))
                    || p.Variants.Any(v => v.Ram != null && v.Ram.Contains(filter.RamFilter)));

            if (!string.IsNullOrWhiteSpace(filter.CpuFilter))
                query = query.Where(p =>
                    p.Tags!.Any(t => t.TagKey == "CPU" && t.TagValue.Contains(filter.CpuFilter))
                    || p.Variants.Any(v => v.Cpu != null && v.Cpu.Contains(filter.CpuFilter)));

            if (!string.IsNullOrWhiteSpace(filter.VgaFilter))
                query = query.Where(p =>
                    p.Tags!.Any(t => t.TagKey == "VGA" && t.TagValue.Contains(filter.VgaFilter))
                    || p.Variants.Any(v => v.Gpu != null && v.Gpu.Contains(filter.VgaFilter)));

            return query;
        }

        // ── Sort + Pagination ─────────────────────────────────────
        private static IQueryable<Product> ApplySortAndPage(
            IQueryable<Product> query,
            ProductFilterModel filter)
        {
            query = filter.SortBy switch
            {
                "price_asc"  => query.OrderBy(p => p.Variants
                                    .Where(v => v.IsActive)
                                    .Select(v => (decimal?)v.Price).Min() ?? 0m),
                "price_desc" => query.OrderByDescending(p => p.Variants
                                    .Where(v => v.IsActive)
                                    .Select(v => (decimal?)v.Price).Max() ?? 0m),
                "name_asc"   => query.OrderBy(p => p.Name),
                "name_desc"  => query.OrderByDescending(p => p.Name),
                "new"        => query.OrderByDescending(p => p.CreatedAt),
                _            => query.OrderByDescending(p => p.CreatedAt)
            };

            var skip = (filter.Page - 1) * filter.PageSize;
            return query.Skip(skip).Take(filter.PageSize);
        }

        // ── Filter options (brand, cpu, vga, ram, price range) ────
        private async Task<FilterOptions> ComputeFilterOptionsAsync(ProductFilterModel filter)
        {
            var baseQuery = BuildBaseQuery(new ProductFilterModel
            {
                CategoryId    = filter.CategoryId,
                SearchKeyword = filter.SearchKeyword,
                MinPrice      = filter.MinPrice,
                MaxPrice      = filter.MaxPrice,
                Status        = filter.Status,
            });

            var productIds = await baseQuery.Select(p => p.Id).ToListAsync();
            if (!productIds.Any()) return new FilterOptions();

            var brands = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .GroupBy(p => new { p.BrandId, p.Brand!.Name })
                .Select(g => new BrandOption
                {
                    Id    = g.Key.BrandId,
                    Name  = g.Key.Name,
                    Count = g.Count()
                })
                .OrderBy(b => b.Name)
                .ToListAsync();

            // CPU/VGA/RAM có thể ở ProductTag hoặc Variant — ưu tiên lấy từ Variant
            var cpuOptions = await _context.ProductVariants
                .Where(v => productIds.Contains(v.ProductId) && v.Cpu != null && v.Cpu != "")
                .Select(v => v.Cpu!)
                .Distinct().OrderBy(s => s).ToListAsync();

            var vgaOptions = await _context.ProductVariants
                .Where(v => productIds.Contains(v.ProductId) && v.Gpu != null && v.Gpu != "")
                .Select(v => v.Gpu!)
                .Distinct().OrderBy(s => s).ToListAsync();

            var ramOptions = await _context.ProductVariants
                .Where(v => productIds.Contains(v.ProductId) && v.Ram != null && v.Ram != "")
                .Select(v => v.Ram!)
                .Distinct().OrderBy(s => s).ToListAsync();

            // Price range lấy từ Variants của các product đang có mặt
            var priceStats = await _context.ProductVariants
                .Where(v => productIds.Contains(v.ProductId) && v.IsActive)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Min = g.Min(v => v.Price),
                    Max = g.Max(v => v.Price)
                })
                .FirstOrDefaultAsync();

            return new FilterOptions
            {
                AvailableBrands = brands,
                AvailableCpus   = cpuOptions,
                AvailableVgas   = vgaOptions,
                AvailableRams   = ramOptions,
                MinPriceInSet   = priceStats?.Min ?? 0,
                MaxPriceInSet   = priceStats?.Max ?? 0
            };
        }
    }
}

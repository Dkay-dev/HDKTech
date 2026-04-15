using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(HDKTechContext context) : base(context) { }

        // ─────────────────────────────────────────────────────────────────────
        // FIX 1: Related Products — phải Include đầy đủ Images, Brand, Category
        // để Partial _ProductCard.cshtml có dữ liệu render
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<Product>> GetRelatedProductsAsync(
            int currentProductId,
            int categoryId,
            int limit)
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
        // FIX 2: FilterProductsAsync — hỗ trợ đầy đủ các tham số sidebar
        // Logic đặc biệt: khi lọc theo categoryId, tự động bao gồm tất cả
        // sản phẩm thuộc danh mục con (đệ quy) của danh mục đó.
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<Product>> FilterProductsAsync(ProductFilterModel filter)
        {
            var query = _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .AsQueryable();

            // ── Lọc theo danh mục (bao gồm danh mục con đệ quy) ─────────────
            if (filter.CategoryId.HasValue && filter.CategoryId > 0)
            {
                var allCategoryIds = await GetAllDescendantCategoryIds(filter.CategoryId.Value);
                query = query.Where(p => allCategoryIds.Contains(p.CategoryId));
            }

            // ── Lọc theo thương hiệu ──────────────────────────────────────────
            if (filter.BrandId.HasValue && filter.BrandId > 0)
            {
                query = query.Where(p => p.BrandId == filter.BrandId.Value);
            }

            // ── Lọc theo giá ──────────────────────────────────────────────────
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            }
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }

            // ── Lọc theo trạng thái tồn kho ───────────────────────────────────
            if (filter.Status.HasValue)
            {
                query = query.Where(p => p.Status == filter.Status.Value);
            }

            // ── Lọc theo từ khóa tìm kiếm ─────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                var kw = filter.SearchKeyword.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(kw) ||
                    (p.Description != null && p.Description.ToLower().Contains(kw)));
            }

            // ── Lọc theo dòng CPU ─────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(filter.CpuLine))
            {
                var cpu = filter.CpuLine;
                query = query.Where(p => p.Specifications != null &&
                                         p.Specifications.Contains(cpu));
            }

            // ── Lọc theo dòng VGA ─────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(filter.VgaLine))
            {
                var vga = filter.VgaLine;
                query = query.Where(p => p.Specifications != null &&
                                         p.Specifications.Contains(vga));
            }

            // ── Lọc theo loại RAM ─────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(filter.RamType))
            {
                var ram = filter.RamType;
                query = query.Where(p => p.Specifications != null &&
                                         p.Specifications.Contains(ram));
            }

            // ── Sắp xếp ──────────────────────────────────────────────────────
            query = ApplySortBy(query, filter.SortBy);

            return await query.ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: Lấy ID của tất cả danh mục con (đệ quy) tính từ một root
        // Trả về danh sách bao gồm cả ID của danh mục gốc được truyền vào.
        // ─────────────────────────────────────────────────────────────────────
        private async Task<List<int>> GetAllDescendantCategoryIds(int rootCategoryId)
        {
            var result = new List<int> { rootCategoryId };
            var queue = new Queue<int>();
            queue.Enqueue(rootCategoryId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var children = await _context.Categories
                    .Where(c => c.ParentCategoryId == currentId)
                    .Select(c => c.Id)
                    .ToListAsync();

                foreach (var childId in children)
                {
                    if (!result.Contains(childId))
                    {
                        result.Add(childId);
                        queue.Enqueue(childId);
                    }
                }
            }

            return result;
        }

        private IQueryable<Product> ApplySortBy(IQueryable<Product> query, string? sortBy)
        {
            return sortBy?.ToLower() switch
            {
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "new" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Các phương thức hiện có — giữ nguyên, không thay đổi
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<Product>> GetAllWithImagesAsync()
        {
            return await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .ToListAsync();
        }

        public async Task<Product?> GetProductWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .Include(p => p.Inventories)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<string>> GetUniqueBrandsByCategory(int categoryId)
        {
            var query = _dbSet.Include(p => p.Brand).AsQueryable();
            if (categoryId > 0)
                query = query.Where(p => p.CategoryId == categoryId);

            return await query
                .Where(p => p.Brand != null)
                .Select(p => p.Brand!.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();
        }

        public async Task<List<string>> GetUniqueCpuLines()
        {
            var specs = await _dbSet
                .Where(p => p.Specifications != null && p.Specifications != "")
                .Select(p => p.Specifications!)
                .Distinct()
                .ToListAsync();

            return ExtractUniqueCpuLines(specs);
        }

        private List<string> ExtractUniqueCpuLines(List<string> specs)
        {
            var cpuLines = new HashSet<string>();
            var patterns = new[]
            {
                "i3", "i5", "i7", "i9",
                "Ryzen 3", "Ryzen 5", "Ryzen 7", "Ryzen 9"
            };

            foreach (var spec in specs)
            {
                foreach (var pattern in patterns)
                {
                    if (spec?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true)
                        cpuLines.Add(pattern);
                }
            }

            return cpuLines.OrderBy(s => s).ToList();
        }

        public async Task<List<Product>> GetFlashSaleProductsAsync(int limit = 5)
        {
            var now = DateTime.Now;
            return await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .Where(p =>
                    p.IsFlashSale &&
                    p.FlashSalePrice.HasValue &&
                    p.FlashSalePrice < p.Price &&
                    p.FlashSaleEndTime.HasValue &&
                    p.FlashSaleEndTime.Value > now)
                .OrderBy(p => p.FlashSaleEndTime)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Product>> GetTopSellerProductsAsync(int limit = 8)
        {
            return await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .OrderByDescending(p => p.Id)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Product>> GetNewProductsAsync(int limit = 6)
        {
            return await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
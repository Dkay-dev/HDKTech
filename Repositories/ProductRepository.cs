using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(HDKTechContext context) : base(context) { }

        public async Task<List<Product>> GetAllWithImagesAsync()
        {
            return await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .ToListAsync();
        }

        public async Task<Product?> GetProductWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<Product>> GetRelatedProductsAsync(int categoryId, int currentProductId, int limit)
        {
            return await _dbSet
                .Where(p => p.CategoryId == categoryId && p.Id != currentProductId)
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Product>> FilterProductsAsync(ProductFilterModel filter)
        {
            var query = _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .AsQueryable();

            // Filter by category
            if (filter.CategoryId.HasValue && filter.CategoryId > 0)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId);
            }

            // Filter by brand
            if (filter.BrandId.HasValue && filter.BrandId > 0)
            {
                query = query.Where(p => p.BrandId == filter.BrandId);
            }

            // Filter by price
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice);
            }

            // Filter by status
            if (filter.Status.HasValue)
            {
                query = query.Where(p => p.Status == filter.Status);
            }

            // Filter by search keyword
            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                query = query.Where(p => p.Name.Contains(filter.SearchKeyword));
            }

            // Filter by CPU
            if (!string.IsNullOrWhiteSpace(filter.CpuLine))
            {
                query = query.Where(p => p.Specifications != null && p.Specifications.Contains(filter.CpuLine));
            }

            // Filter by VGA
            if (!string.IsNullOrWhiteSpace(filter.VgaLine))
            {
                query = query.Where(p => p.Specifications != null && p.Specifications.Contains(filter.VgaLine));
            }

            // Filter by RAM type
            if (!string.IsNullOrWhiteSpace(filter.RamType))
            {
                query = query.Where(p => p.Specifications != null && p.Specifications.Contains(filter.RamType));
            }

            // Sort
            query = ApplySortBy(query, filter.SortBy);

            return await query.ToListAsync();
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
                _ => query.OrderByDescending(p => p.CreatedAt) // Default: newest
            };
        }

        public async Task<List<string>> GetUniqueBrandsByCategory(int categoryId)
        {
            return await _dbSet
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Brand)
                .Select(p => p.Brand.Name)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<string>> GetUniqueCpuLines()
        {
            return await _dbSet
                .Where(p => p.Specifications != null)
                .Select(p => p.Specifications)
                .Distinct()
                .ToListAsync()
                .ContinueWith(t => ExtractUniqueCpuLines(t.Result));
        }

        private List<string> ExtractUniqueCpuLines(List<string>? specs)
        {
            var cpuLines = new HashSet<string>();
            var cpuPatterns = new[] { "i3", "i5", "i7", "i9", "Ryzen 3", "Ryzen 5", "Ryzen 7", "Ryzen 9" };

            if (specs == null) return cpuLines.ToList();

            foreach (var spec in specs)
            {
                foreach (var pattern in cpuPatterns)
                {
                    if (spec?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        cpuLines.Add(pattern);
                    }
                }
            }

            return cpuLines.ToList();
        }

        public async Task<List<Product>> GetFlashSaleProductsAsync(int limit = 5)
        {
            var now = DateTime.Now;
            return await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Where(p =>
                    p.IsFlashSale &&
                    p.FlashSalePrice.HasValue &&
                    p.FlashSalePrice < p.Price &&
                    p.FlashSaleEndTime.HasValue &&
                    p.FlashSaleEndTime.Value > now)
                .OrderBy(p => p.FlashSaleEndTime) // Sắp hết hạn nhất lên đầu
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Product>> GetTopSellerProductsAsync(int limit = 8)
        {
            return await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.Id)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Product>> GetNewProductsAsync(int limit = 6)
        {
            return await _dbSet
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }


    }
}



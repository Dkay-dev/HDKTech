using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    /// <summary>
    /// Repository xử lý toàn bộ nghiệp vụ dữ liệu cho Danh Mục sản phẩm.
    /// Kế thừa GenericRepository và triển khai ICategoryRepository.
    /// </summary>
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(HDKTechContext context, ILogger<CategoryRepository> logger)
            : base(context)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<List<Category>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(c => c.Products)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<Category>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(c => c.Products)
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                    .ThenInclude(sc => sc.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<Category>> GetParentCategoriesAsync(int? excludeId = null)
        {
            var query = _dbSet
                .AsNoTracking()
                .Where(c => c.ParentCategoryId == null);

            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);

            return await query.OrderBy(c => c.Name).ToListAsync();
        }

        /// <inheritdoc/>
        public new async Task<Category?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <inheritdoc/>
        public async Task<Category?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(c => c.Products)
                    .ThenInclude(p => p.Images)
                .Include(c => c.Products)
                    .ThenInclude(p => p.Inventories)
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                    .ThenInclude(sc => sc.Products)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <inheritdoc/>
        public new async Task<bool> AddAsync(Category category)
        {
            try
            {
                await _dbSet.AddAsync(category);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm danh mục: {CategoryName}", category.Name);
                return false;
            }
        }

        /// <inheritdoc/>
        public new async Task<bool> UpdateAsync(Category category)
        {
            try
            {
                _dbSet.Update(category);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Lỗi concurrency khi cập nhật danh mục Id: {Id}", category.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật danh mục Id: {Id}", category.Id);
                return false;
            }
        }

        /// <inheritdoc/>
        public new async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var category = await _dbSet.FindAsync(id);
                if (category == null) return false;

                _dbSet.Remove(category);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa danh mục Id: {Id}", id);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> HasProductsAsync(int categoryId)
        {
            return await _context.Products
                .AnyAsync(p => p.CategoryId == categoryId);
        }

        /// <inheritdoc/>
        public async Task<bool> HasSubCategoriesAsync(int categoryId)
        {
            return await _dbSet
                .AnyAsync(c => c.ParentCategoryId == categoryId);
        }

        /// <inheritdoc/>
        public async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        /// <inheritdoc/>
        public async Task<int> CountEmptyAsync()
        {
            return await _dbSet
                .Where(c => !c.Products.Any())
                .CountAsync();
        }

        // ──────────── Các phương thức hỗ trợ (giữ nguyên từ bản cũ) ────────────

        public async Task<Category?> GetByIdWithProductsAsync(int id)
        {
            return await _dbSet
                .Include(c => c.Products)
                    .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Category>> GetAllWithProductsAsync()
        {
            return await _dbSet
                .Include(c => c.Products)
                .ToListAsync();
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            // Lấy tất cả danh mục con (recursive) của categoryId
            var allChildIds = new List<int> { categoryId };
            var queue = new Queue<int>();
            queue.Enqueue(categoryId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var children = await _context.Categories
                    .Where(d => d.ParentCategoryId == currentId)
                    .Select(d => d.Id)
                    .ToListAsync();

                foreach (var childId in children)
                {
                    if (!allChildIds.Contains(childId))
                    {
                        allChildIds.Add(childId);
                        queue.Enqueue(childId);
                    }
                }
            }

            // Lấy sản phẩm từ tất cả danh mục (cha + con + con của con...)
            return await _context.Products
                .Where(p => allChildIds.Contains(p.CategoryId))
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}



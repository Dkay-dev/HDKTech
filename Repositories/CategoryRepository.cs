using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class CategoryRepository : GenericRepository<Category>
    {
        public CategoryRepository(HDKTechContext context) : base(context) { }

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
                .Where(p => allChildIds.Contains(p.Id))
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}



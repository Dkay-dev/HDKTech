using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class BrandRepository : GenericRepository<Brand>, IBrandRepository
    {
        private readonly ILogger<BrandRepository> _logger;

        public BrandRepository(HDKTechContext context, ILogger<BrandRepository> logger) : base(context)
        {
            _logger = logger;
        }

        public new async Task<List<Brand>> GetAllAsync()
            => await _dbSet.AsNoTracking().Include(b => b.Products).OrderBy(b => b.Name).ToListAsync();

        public async Task<List<Brand>> GetAllWithProductCountAsync()
            => await _dbSet.AsNoTracking().Include(b => b.Products).OrderBy(b => b.Name).ToListAsync();

        public new async Task<Brand?> GetByIdAsync(int id)
            => await _dbSet.Include(b => b.Products).FirstOrDefaultAsync(b => b.Id == id);

        public async Task<Brand?> GetByIdWithProductsAsync(int id)
            => await _dbSet
                .Include(b => b.Products).ThenInclude(p => p.Images)
                .Include(b => b.Products).ThenInclude(p => p.Variants).ThenInclude(v => v.Inventories)
                .FirstOrDefaultAsync(b => b.Id == id);

        public new async Task<bool> AddAsync(Brand brand)
        {
            try { await _dbSet.AddAsync(brand); return await _context.SaveChangesAsync() > 0; }
            catch (Exception ex) { _logger.LogError(ex, "Lỗi AddBrand: {Name}", brand.Name); return false; }
        }

        public new async Task<bool> UpdateAsync(Brand brand)
        {
            try { _dbSet.Update(brand); return await _context.SaveChangesAsync() > 0; }
            catch (Exception ex) { _logger.LogError(ex, "Lỗi UpdateBrand Id: {Id}", brand.Id); return false; }
        }

        public new async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var brand = await _dbSet.FindAsync(id);
                if (brand == null) return false;
                _dbSet.Remove(brand);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex) { _logger.LogError(ex, "Lỗi DeleteBrand Id: {Id}", id); return false; }
        }

        public async Task<bool> HasProductsAsync(int brandId)
            => await _context.Products.AnyAsync(p => p.BrandId == brandId);

        public async Task<int> CountAsync() => await _dbSet.CountAsync();

        public async Task<int> CountEmptyAsync()
            => await _dbSet.Where(b => !b.Products.Any()).CountAsync();
    }
}

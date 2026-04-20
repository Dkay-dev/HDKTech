using HDKTech.Data;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly HDKTechContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(HDKTechContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<List<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public async Task UpdateAsync(T entity) => _dbSet.Update(entity);

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null) _dbSet.Remove(entity);
        }

        public async Task<bool> SaveAsync() => await _context.SaveChangesAsync() > 0;
    }
}



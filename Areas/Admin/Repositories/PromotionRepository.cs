using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Repositories
{
    public class PromotionRepository
    {
        private readonly HDKTechContext _context;

        public PromotionRepository(HDKTechContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Promotion>> GetAllPromotionsAsync()
        {
            return await _context.Promotions
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Promotion>> GetActivePromotionsAsync()
        {
            var now = DateTime.Now;
            return await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Promotion>> GetPromotionsByStatusAsync(string status)
        {
            return await _context.Promotions
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Promotion> GetPromotionByIdAsync(int id)
        {
            return await _context.Promotions.FindAsync(id);
        }

        public async Task<Promotion> GetPromotionByCodeAsync(string code)
        {
            return await _context.Promotions
                .Where(p => p.PromoCode == code && p.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CreatePromotionAsync(Promotion promotion)
        {
            promotion.Status = GetStatus(promotion.StartDate, promotion.EndDate);
            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();
            return promotion.Id;
        }

        public async Task UpdatePromotionAsync(Promotion promotion)
        {
            promotion.UpdatedAt = DateTime.Now;
            promotion.Status = GetStatus(promotion.StartDate, promotion.EndDate);
            _context.Promotions.Update(promotion);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePromotionAsync(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdatePromotionUsageAsync(int id, int usage)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null)
            {
                promotion.UsageCount = usage;
                promotion.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        private string GetStatus(DateTime start, DateTime end)
        {
            var now = DateTime.Now;
            if (now < start) return "Scheduled";
            if (now > end) return "Ended";
            return "Running";
        }
    }
}

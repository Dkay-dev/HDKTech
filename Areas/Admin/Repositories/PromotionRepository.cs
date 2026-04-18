using HDKTech.Areas.Admin.Models;
using HDKTech.Data;
using HDKTech.Models;
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
            // Sử dụng thuộc tính IsActive và so sánh thời gian
            return await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now && p.Status == PromotionStatus.Running)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // Đổi tham số truyền vào từ string sang Enum để đồng bộ
        public async Task<IEnumerable<Promotion>> GetPromotionsByStatusAsync(PromotionStatus status)
        {
            return await _context.Promotions
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Promotion?> GetPromotionByIdAsync(int id)
        {
            return await _context.Promotions
                .Include(p => p.PromotionProducts)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Promotion?> GetPromotionByCodeAsync(string code)
        {
            return await _context.Promotions
                .Where(p => p.PromoCode == code && p.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CreatePromotionAsync(Promotion promotion)
        {
            // Cập nhật Status dựa trên Enum thay vì String
            promotion.Status = CalculateStatus(promotion.StartDate, promotion.EndDate);
            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();
            return promotion.Id;
        }

        public async Task UpdatePromotionAsync(Promotion promotion)
        {
            promotion.UpdatedAt = DateTime.Now;
            promotion.Status = CalculateStatus(promotion.StartDate, promotion.EndDate);
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

        // Sửa hàm này để trả về Enum PromotionStatus
        private PromotionStatus CalculateStatus(DateTime start, DateTime end)
        {
            var now = DateTime.Now;
            if (now < start) return PromotionStatus.Scheduled;
            if (now > end) return PromotionStatus.Ended;
            return PromotionStatus.Running;
        }
    }
}
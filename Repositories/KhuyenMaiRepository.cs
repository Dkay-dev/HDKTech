using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class KhuyenMaiRepository
    {
        private readonly HDKTechContext _context;

        public KhuyenMaiRepository(HDKTechContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<KhuyenMai>> GetAllPromotionsAsync()
        {
            return await _context.KhuyenMais
                .OrderByDescending(k => k.NgayTao)
                .ToListAsync();
        }

        public async Task<IEnumerable<KhuyenMai>> GetActivePromotionsAsync()
        {
            var now = DateTime.Now;
            return await _context.KhuyenMais
                .Where(k => k.IsActive && k.NgayBatDau <= now && k.NgayKetThuc >= now)
                .OrderByDescending(k => k.NgayTao)
                .ToListAsync();
        }

        public async Task<IEnumerable<KhuyenMai>> GetPromotionsByStatusAsync(string status)
        {
            return await _context.KhuyenMais
                .Where(k => k.TrangThai == status)
                .OrderByDescending(k => k.NgayTao)
                .ToListAsync();
        }

        public async Task<KhuyenMai> GetPromotionByIdAsync(int id)
        {
            return await _context.KhuyenMais.FindAsync(id);
        }

        public async Task<KhuyenMai> GetPromotionByCodeAsync(string code)
        {
            return await _context.KhuyenMais
                .Where(k => k.MaKhuyenMai_Code == code && k.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CreatePromotionAsync(KhuyenMai khuyenMai)
        {
            khuyenMai.TrangThai = GetStatus(khuyenMai.NgayBatDau, khuyenMai.NgayKetThuc);
            _context.KhuyenMais.Add(khuyenMai);
            await _context.SaveChangesAsync();
            return khuyenMai.MaKhuyenMai;
        }

        public async Task UpdatePromotionAsync(KhuyenMai khuyenMai)
        {
            khuyenMai.NgayCapNhat = DateTime.Now;
            khuyenMai.TrangThai = GetStatus(khuyenMai.NgayBatDau, khuyenMai.NgayKetThuc);
            _context.KhuyenMais.Update(khuyenMai);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePromotionAsync(int id)
        {
            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai != null)
            {
                _context.KhuyenMais.Remove(khuyenMai);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdatePromotionUsageAsync(int id, int usage)
        {
            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai != null)
            {
                khuyenMai.SoLuongSuDung = usage;
                khuyenMai.NgayCapNhat = DateTime.Now;
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

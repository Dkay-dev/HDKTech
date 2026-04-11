using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class BannerRepository
    {
        private readonly HDKTechContext _context;

        public BannerRepository(HDKTechContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Banner>> GetAllBannersAsync()
        {
            return await _context.Banners
                .OrderBy(b => b.ThuTuHienThi)
                .ToListAsync();
        }

        public async Task<IEnumerable<Banner>> GetActiveBannersAsync()
        {
            return await _context.Banners
                .Where(b => b.IsActive)
                .OrderBy(b => b.ThuTuHienThi)
                .ToListAsync();
        }

        public async Task<IEnumerable<Banner>> GetBannersByTypeAsync(string loaiBanner)
        {
            return await _context.Banners
                .Where(b => b.LoaiBanner == loaiBanner && b.IsActive)
                .OrderBy(b => b.ThuTuHienThi)
                .ToListAsync();
        }

        public async Task<Banner> GetBannerByIdAsync(int id)
        {
            return await _context.Banners.FindAsync(id);
        }

        public async Task<int> CreateBannerAsync(Banner banner)
        {
            _context.Banners.Add(banner);
            await _context.SaveChangesAsync();
            return banner.MaBanner;
        }

        public async Task UpdateBannerAsync(Banner banner)
        {
            banner.NgayCapNhat = DateTime.Now;
            _context.Banners.Update(banner);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBannerAsync(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateBannerOrderAsync(List<(int BannerId, int Order)> orderUpdates)
        {
            foreach (var (bannerId, order) in orderUpdates)
            {
                var banner = await _context.Banners.FindAsync(bannerId);
                if (banner != null)
                {
                    banner.ThuTuHienThi = order;
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}

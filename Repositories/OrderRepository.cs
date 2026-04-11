using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    public class OrderRepository : GenericRepository<DonHang>, IOrderRepository
    {
        public OrderRepository(HDKTechContext context) : base(context)
        {
        }

        public async Task<DonHang> CreateOrderAsync(string userId, string tenNguoiNhan, string soDienThoai, 
                                                    string diaChiGiaoHang, List<CartItem> items, decimal phiVanChuyen = 0)
        {
            // Tính tổng tiền
            var tongTien = items.Sum(x => x.Price * x.Quantity);

            // Tạo mã đơn hàng: HDK + timestamp + random 4 digits
            // Ví dụ: HDK20260410180530_4821
            var maDonHangChuoi = $"HDK{DateTime.Now:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";

            // Đảm bảo mã đơn hàng unique (retries 3 lần nếu trùng)
            var retries = 3;
            while (retries-- > 0)
            {
                var existingOrder = await _context.Set<DonHang>()
                    .FirstOrDefaultAsync(x => x.MaDonHangChuoi == maDonHangChuoi);

                if (existingOrder == null)
                    break; // Mã unique, dùng được

                // Nếu trùng, tạo mã mới
                maDonHangChuoi = $"HDK{DateTime.Now:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
            }

            var donHang = new DonHang
            {
                MaNguoiDung = userId,
                MaDonHangChuoi = maDonHangChuoi,
                TenNguoiNhan = tenNguoiNhan,
                SoDienThoaiNhan = soDienThoai,
                DiaChiGiaoHang = diaChiGiaoHang,
                TongTien = tongTien,
                PhiVanChuyen = phiVanChuyen,
                TrangThaiDonHang = 0, // Chờ xác nhận
                NgayDatHang = DateTime.Now,
                ChiTietDonHangs = new List<ChiTietDonHang>()
            };

            // Thêm chi tiết đơn hàng
            foreach (var item in items)
            {
                var chiTiet = new ChiTietDonHang
                {
                    MaSanPham = item.ProductId,
                    SoLuong = item.Quantity,
                    GiaBanLucMua = item.Price
                };
                donHang.ChiTietDonHangs.Add(chiTiet);
            }

            // Lưu vào database
            await _context.AddAsync(donHang);
            await _context.SaveChangesAsync();

            return donHang;
        }

        public async Task<DonHang> GetOrderByMaDonHangAsync(string maDonHangChuoi)
        {
            return await _context.Set<DonHang>()
                .Include(x => x.ChiTietDonHangs)
                .Include(x => x.NguoiDung)
                .FirstOrDefaultAsync(x => x.MaDonHangChuoi == maDonHangChuoi);
        }

        public async Task<IEnumerable<DonHang>> GetUserOrdersAsync(string userId)
        {
            return await _context.Set<DonHang>()
                .Include(x => x.ChiTietDonHangs)
                .Where(x => x.MaNguoiDung == userId)
                .OrderByDescending(x => x.NgayDatHang)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int maDonHang, int trangThaiMoi)
        {
            var donHang = await _context.Set<DonHang>().FindAsync(maDonHang);
            if (donHang == null)
                return false;

            donHang.TrangThaiDonHang = trangThaiMoi;
            _context.Update(donHang);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOrderAsync(int maDonHang)
        {
            var donHang = await _context.Set<DonHang>()
                .Include(x => x.ChiTietDonHangs)
                .FirstOrDefaultAsync(x => x.MaDonHang == maDonHang);

            if (donHang == null)
                return false;

            _context.RemoveRange(donHang.ChiTietDonHangs);
            _context.Remove(donHang);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

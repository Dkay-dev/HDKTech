using HDKTech.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data;

public class HDKTechContext : IdentityDbContext<NguoiDung>
{
    public HDKTechContext(DbContextOptions<HDKTechContext> options)
        : base(options)
    {
    }
    public DbSet<SanPham> SanPhams { get; set; }
    public DbSet<DanhMuc> DanhMucs { get; set; }
    public DbSet<DanhGia> DanhGias { get; set; }
    public DbSet<HangSX> HangSXs { get; set; }
    public DbSet<KhoHang> KhoHangs { get; set; }
    public DbSet<DonHang> DonHangs { get; set; }
    public DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }
    public DbSet<HoaDon> HoaDons { get; set; }

    // GioHang entities - Không sử dụng nữa (dùng Session-based cart)
    // public DbSet<GioHang> GioHangs { get; set; }
    // public DbSet<ChiTietGioHang> ChiTietGioHangs { get; set; }

    public DbSet<NhatKyHeThong> NhatKyHeThongs { get; set; }
    public DbSet<PhienChat> PhienChats { get; set; }
    public DbSet<TinNhanChat> TinNhanChats { get; set; }
    public DbSet<YeuCauOTP> YeuCauOTPs { get; set; }
    public DbSet<HinhAnh> HinhAnhs { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<BannerClickEvent> BannerClickEvents { get; set; }
    public DbSet<KhuyenMai> KhuyenMais { get; set; }
    public DbSet<SystemLog> SystemLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        //quan he 1-1 don hang va hoa don
        builder.Entity<HoaDon>()
                .HasOne(h => h.DonHang)
                .WithOne(d => d.HoaDon)
                .HasForeignKey<HoaDon>(h => h.MaDonHang)
                .OnDelete(DeleteBehavior.Cascade);
        //quan he cha con cho danh muc 
        builder.Entity<PhienChat>()
                .HasOne(p => p.KhachHang)
                .WithMany()
                .HasForeignKey(p => p.MaKhachHang)
                .OnDelete(DeleteBehavior.Restrict);
        //Cau hinh PhienChat
        builder.Entity<PhienChat>()
                .HasOne(p => p.KhachHang)
                .WithMany()
                .HasForeignKey(p => p.MaKhachHang)
                .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PhienChat>()
            .HasOne(p => p.NhanVien)
            .WithMany()
            .HasForeignKey(p => p.MaNhanVien)
            .OnDelete(DeleteBehavior.Restrict);
        //quan he 1-1 sanpham kho hang
        builder.Entity<KhoHang>()
                .HasOne(k => k.SanPham)
                .WithOne()
                .HasForeignKey<KhoHang>(k => k.MaSanPham);
        foreach (var property in builder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        // HinhAnh relationship
        // HinhAnh mapping and relationship
        builder.Entity<HinhAnh>()
            .ToTable("HinhAnh");

        builder.Entity<HinhAnh>()
            .HasOne(h => h.SanPham)
            .WithMany(s => s.HinhAnhs)
            .HasForeignKey(h => h.MaSanPham)
            .OnDelete(DeleteBehavior.Cascade);
    }

}

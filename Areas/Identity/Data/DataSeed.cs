using HDKTech.ChucNangPhanQuyen;
using HDKTech.Models;
using Microsoft.AspNetCore.Identity;

namespace HDKTech.Areas.Identity.Data
{
    public class DataSeed
    {
        public static async Task KhoiTaoDuLieuMacDinh(IServiceProvider dichVu)
        {
            var quanLyNguoiDung = dichVu.GetService<UserManager<NguoiDung>>();
            var quanLyVaiTro = dichVu.GetService<RoleManager<IdentityRole>>();

           
            foreach (var vaiTro in Enum.GetNames(typeof(PhanQuyen)))
            {
                if (!await quanLyVaiTro.RoleExistsAsync(vaiTro))
                {
                    await quanLyVaiTro.CreateAsync(new IdentityRole(vaiTro));
                }
            }

          
            var emailAdmin = "admin@gmail.com";
            var adminTrongDb = await quanLyNguoiDung.FindByEmailAsync(emailAdmin);
            if (adminTrongDb is null)
            {
                var admin = new NguoiDung
                {
                    UserName = emailAdmin,
                    Email = emailAdmin,
                    EmailConfirmed = true,
                    HoTen = "Phan Đình Đại",
                    NgayTao = DateTime.Now
                };

                var ketQuaAdmin = await quanLyNguoiDung.CreateAsync(admin, "Admin123@");
                if (ketQuaAdmin.Succeeded)
                {
                    await quanLyNguoiDung.AddToRoleAsync(admin, PhanQuyen.Admin.ToString());
                }
            }

           
            var emailNhanVien = "nhanvien@gmail.com";
            var nvTrongDb = await quanLyNguoiDung.FindByEmailAsync(emailNhanVien);
            if (nvTrongDb is null)
            {
                var nhanVien = new NguoiDung
                {
                    UserName = emailNhanVien,
                    Email = emailNhanVien,
                    EmailConfirmed = true,
                    HoTen = "Lê Ý Thiên",
                    NgayTao = DateTime.Now
                };

                var ketQuaNV = await quanLyNguoiDung.CreateAsync(nhanVien, "Nhanvien123@");
                if (ketQuaNV.Succeeded)
                {
                    await quanLyNguoiDung.AddToRoleAsync(nhanVien, PhanQuyen.NhanVien.ToString());
                }
            }
        }
    }
}
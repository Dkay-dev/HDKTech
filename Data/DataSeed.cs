using Bogus;
using HDKTech.ChucNangPhanQuyen;
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HDKTech.Areas.Identity.Data
{
    public static class DataSeed
    {
        public static async Task KhoiTaoDuLieuMacDinh(IServiceProvider services)
        {
            var context = services.GetRequiredService<HDKTechContext>();
            var userManager = services.GetRequiredService<UserManager<NguoiDung>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var faker = new Faker("vi");
            var random = new Random();

            #region 1. ROLES & USERS
            foreach (var role in Enum.GetNames(typeof(PhanQuyen)))
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
            async Task<NguoiDung> TaoUser(string email, string ten, string role)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new NguoiDung { UserName = email, Email = email, EmailConfirmed = true, HoTen = ten, NgayTao = DateTime.Now };
                    await userManager.CreateAsync(user, "123456Aa@");
                    await userManager.AddToRoleAsync(user, role);
                }
                return user;
            }
            var admin = await TaoUser("admin@gmail.com", "Admin HDK", PhanQuyen.Admin.ToString());
            #endregion

            #region 2. HÃNG SẢN XUẤT (Full Brands thực tế)
            if (!await context.HangSXs.AnyAsync())
            {
                var brands = new[] { "ASUS", "MSI", "GIGABYTE", "LENOVO", "ACER", "DELL", "APPLE", "INTEL", "AMD", "NVIDIA", "LOGITECH", "RAZER", "SAMSUNG", "CORSAIR", "KINGSTON", "NZXT", "LIAN LI", "AKKO", "DARE-U", "EDIFIER", "HYPERX", "STEELSERIES" };
                foreach (var b in brands)
                    context.HangSXs.Add(new HangSX { TenHangSX = b, MoTa = $"Thương hiệu hàng đầu {b}" });
                await context.SaveChangesAsync();
            }
            #endregion

            #region 3. DANH MỤC "VÔ CỰC" (Bám sát 100% Sidebar GearVN)
            if (!await context.DanhMucs.AnyAsync())
            {
                // --- TẦNG 1: SIDEBAR ---
                var dm1 = new DanhMuc { TenDanhMuc = "Laptop", MoTaDanhMuc = "Máy tính xách tay mạnh mẽ cho công việc và giải trí hàng ngày", BannerImageUrl = "/images/banners/category-1.jpg" };
                var dm2 = new DanhMuc { TenDanhMuc = "Laptop Gaming", MoTaDanhMuc = "Gaming laptop với hiệu năng cao cho các tựa game AAA", BannerImageUrl = "/images/banners/category-2.jpg" };
                var dm3 = new DanhMuc { TenDanhMuc = "PC GVN", MoTaDanhMuc = "PC chuyên dụng cho thiết kế đồ họa, video editing và content creation", BannerImageUrl = "/images/banners/category-3.jpg" };
                var dm4 = new DanhMuc { TenDanhMuc = "Main, CPU, VGA", MoTaDanhMuc = "Bộ xử lý, mainboard, card đồ họa và các linh kiện chính", BannerImageUrl = "/images/banners/category-4.jpg" };
                var dm5 = new DanhMuc { TenDanhMuc = "Case, Nguồn, Tản", MoTaDanhMuc = "Vỏ máy, nguồn điện và tản nhiệt chất lượng cao", BannerImageUrl = "/images/banners/category-5.jpg" };
                var dm6 = new DanhMuc { TenDanhMuc = "Ổ cứng, RAM, Thẻ nhớ", MoTaDanhMuc = "Ổ cứng, RAM, thẻ nhớ và các thiết bị lưu trữ", BannerImageUrl = "/images/banners/category-6.jpg" };
                var dm7 = new DanhMuc { TenDanhMuc = "Loa, Micro, Webcam", MoTaDanhMuc = "Tai nghe, loa và các thiết bị âm thanh chuyên dụng", BannerImageUrl = "/images/banners/category-7.jpg" };
                var dm8 = new DanhMuc { TenDanhMuc = "Màn hình", MoTaDanhMuc = "Màn hình máy tính với độ phân giải và tốc độ cao", BannerImageUrl = "/images/banners/category-8.jpg" };
                var dm9 = new DanhMuc { TenDanhMuc = "Bàn phím", MoTaDanhMuc = "Chuột, bàn phím, tay cầm và các phụ kiện máy tính", BannerImageUrl = "/images/banners/category-9.jpg" };
                var dm10 = new DanhMuc { TenDanhMuc = "Chuột + Lót chuột", MoTaDanhMuc = "Chuột gaming và phụ kiện đi kèm với độ bền cao", BannerImageUrl = "/images/banners/category-10.jpg" };
                var dm11 = new DanhMuc { TenDanhMuc = "Tai nghe", MoTaDanhMuc = "Tai nghe gaming với âm thanh sống động", BannerImageUrl = "/images/banners/category-11.jpg" };
                var dm12 = new DanhMuc { TenDanhMuc = "Handheld, Console", MoTaDanhMuc = "Máy chơi game cầm tay và thiết bị giải trí di động" };

                context.DanhMucs.AddRange(dm1, dm2, dm3, dm4, dm5, dm6, dm7, dm8, dm9, dm10, dm11, dm12);
                await context.SaveChangesAsync();

                // --- TẦNG 2 & 3: LÀM ĐẦY ĐỦ CỰC CHI TIẾT THEO ẢNH ---

                // 1. Laptop
                await SeedSub(context, dm1, "Thương hiệu", new[] { "ASUS", "DELL", "HP", "LENOVO", "Apple MacBook" });
                await SeedSub(context, dm1, "Giá bán", new[] { "Dưới 15 triệu", "15 - 20 triệu", "Trên 20 triệu" });
                await SeedSub(context, dm1, "CPU Intel", new[] { "Core i3", "Core i5", "Core i7", "Core Ultra (Mới)" });

                // 2. Laptop Gaming
                await SeedSub(context, dm2, "Thương hiệu Gaming", new[] { "ASUS ROG", "MSI Katana", "Acer Predator", "Lenovo Legion" });
                await SeedSub(context, dm2, "VGA rời", new[] { "RTX 4050", "RTX 4060", "RTX 4070", "RTX 50 Series" });

                // 3. PC GVN
                await SeedSub(context, dm3, "PC Theo giá", new[] { "Dưới 20 triệu", "20 - 50 triệu", "50 - 100 triệu", "Trên 200 triệu" });
                await SeedSub(context, dm3, "PC Theo cấu hình", new[] { "PC RTX 4060", "PC RTX 5080", "PC RTX 5090" });

                // 4. Main, CPU, VGA (Ảnh 2)
                await SeedSub(context, dm4, "VGA RTX 50 Series", new[] { "RTX 5090", "RTX 5080", "RTX 5070 Ti", "RTX 5060" });
                await SeedSub(context, dm4, "CPU Intel Core", new[] { "Core Ultra 9", "Core Ultra 7", "Core i9-14900K" });
                await SeedSub(context, dm4, "Bo mạch chủ Intel", new[] { "Z890 (Mới)", "Z790", "B760", "H610" });

                // 5. Case, Nguồn, Tản (Ảnh 5)
                await SeedSub(context, dm5, "Vỏ máy (Case)", new[] { "Case ASUS", "Case Corsair", "Case Lian Li", "Case NZXT" });
                await SeedSub(context, dm5, "Nguồn máy tính", new[] { "Nguồn ASUS", "Nguồn Corsair", "Nguồn MSI", "Nguồn NZXT" });
                await SeedSub(context, dm5, "Loại tản nhiệt", new[] { "Tản AIO 240mm", "Tản AIO 360mm", "Tản nhiệt khí" });

                // 6. Ổ cứng, RAM (Ảnh 10)
                await SeedSub(context, dm6, "Dung lượng RAM", new[] { "8 GB", "16 GB", "32 GB", "64 GB" });
                await SeedSub(context, dm6, "Dung lượng SSD", new[] { "250GB-256GB", "500GB-512GB", "1TB", "Trên 2TB" });

                // 7. Loa, Micro, Webcam (Ảnh 9)
                await SeedSub(context, dm7, "Thương hiệu loa", new[] { "Edifier", "Razer", "Logitech", "SoundMax" });
                await SeedSub(context, dm7, "Webcam", new[] { "Độ phân giải 4K", "Độ phân giải Full HD", "Độ phân giải 720p" });

                // 8. Màn hình (Ảnh 8)
                await SeedSub(context, dm8, "Độ phân giải", new[] { "Full HD 1080p", "2K 1440p", "4K UHD", "Màn hình 6K" });
                await SeedSub(context, dm8, "Tần số quét", new[] { "144Hz", "165Hz", "240Hz", "360Hz", "540Hz" });

                // 9. Bàn phím (Ảnh 7)
                await SeedSub(context, dm9, "Thương hiệu phím", new[] { "AKKO", "AULA", "Keychron", "Durgod", "Corsair" });
                await SeedSub(context, dm9, "Giá tiền", new[] { "Dưới 1 triệu", "1 triệu - 2 triệu", "Trên 4 triệu" });

                // 10. Chuột (Ảnh 6)
                await SeedSub(context, dm10, "Thương hiệu chuột", new[] { "Logitech", "Razer", "Corsair", "Steelseries" });
                await SeedSub(context, dm10, "Lót chuột", new[] { "Lót chuột Nhỏ", "Lót chuột Vừa", "Lót chuột Lớn" });

                // 11. Tai nghe
                await SeedSub(context, dm11, "Thương hiệu tai nghe", new[] { "HyperX", "Corsair", "Razer", "Logitech", "Steelseries" });
                await SeedSub(context, dm11, "Kiểu tai nghe", new[] { "Over-ear", "Gaming In-ear", "Wireless" });

                // 12. Handheld (Ảnh 3)
                await SeedSub(context, dm12, "Handheld PC", new[] { "ROG Ally X", "MSI Claw", "Legion Go" });
                await SeedSub(context, dm12, "Console", new[] { "Sony PS5 Slim", "Nintendo Switch OLED" });


                await context.SaveChangesAsync();
            }
            #endregion

            #region 4. SẢN PHẨM & KHO (44 SP Thực Tế - Giảm từ 400+ xuống 30-50)
            if (!await context.SanPhams.AnyAsync())
            {
                var brands = await context.HangSXs.ToListAsync();
                var categories = await context.DanhMucs.Where(c => c.MaDanhMucCha == null).ToListAsync();

                // Danh sách 44 sản phẩm thực tế, có ý nghĩa
                var products = new List<(string name, decimal price, decimal? msrp, string categoryName, string brand, string description, string specs, int imageId, int discount)>
                {
                    // ===== LAPTOP ULTRABOOK (6 sản phẩm) =====
                    ("Dell XPS 13 Plus - Intel Core i5 Gen 13", 24990000m, 27990000m, "Laptop", "DELL", "Thiết kế siêu mỏng, hiệu năng mạnh mẽ cho công việc văn phòng", "Intel Core i5 Gen 13 | RAM 8GB | SSD 512GB", 1, 11),
                    ("ASUS VivoBook 14 - AMD Ryzen 5 5500U", 11990000m, 13990000m, "Laptop", "ASUS", "Máy tính xách tay nhẹ nhàng, thích hợp cho du lịch", "AMD Ryzen 5 5500U | RAM 8GB | SSD 512GB", 2, 14),
                    ("Lenovo ThinkPad X1 Carbon - Intel Core i7", 32990000m, 36990000m, "Laptop", "LENOVO", "Dòng máy cao cấp cho doanh nhân chuyên nghiệp", "Intel Core i7 Gen 12 | RAM 16GB | SSD 512GB", 3, 11),
                    ("MacBook Air M3 - 2024", 28990000m, null, "Laptop", "APPLE", "Chip Apple M3 mạnh mẽ, thời lượng pin vô địch", "Apple M3 | RAM 8GB | SSD 256GB | GPU 7-core", 4, 0),
                    ("HP Pavilion 15 - Intel Core i5", 12990000m, 15290000m, "Laptop", "HP", "Laptop giá rẻ, hiệu năng ổn định cho học tập", "Intel Core i5 Gen 11 | RAM 8GB | SSD 256GB", 5, 15),
                    ("Acer Aspire 5 - AMD Ryzen 7", 14990000m, 17990000m, "Laptop", "ACER", "Màn hình 15.6 inch IPS, hiệu năng tốt giá cạnh tranh", "AMD Ryzen 7 5700U | RAM 16GB | SSD 512GB", 6, 17),

                    // ===== LAPTOP GAMING (6 sản phẩm) =====
                    ("ASUS ROG Strix G16 - RTX 4090", 49990000m, 59990000m, "Laptop Gaming", "ASUS", "Laptop gaming flagship, GPU RTX 4090 quái vật", "Intel Core i9 Gen 13 | RTX 4090 | RAM 32GB | SSD 1TB", 7, 17),
                    ("MSI Katana 17 - RTX 4070", 27990000m, 32990000m, "Laptop Gaming", "MSI", "Màn hình 144Hz IPS, hiệu năng gaming mạnh", "Intel Core i7 Gen 12 | RTX 4070 | RAM 16GB | SSD 512GB", 8, 15),
                    ("Acer Predator 18 - RTX 4080", 42990000m, 49990000m, "Laptop Gaming", "ACER", "Màn hình 18 inch khổng lồ, RTX 4080 siêu mạnh", "Intel Core i9 Gen 13 | RTX 4080 | RAM 32GB | SSD 1TB", 9, 14),
                    ("Lenovo Legion 9 Pro - RTX 4090 Laptop", 59990000m, 69990000m, "Laptop Gaming", "LENOVO", "Dòng cao cấp nhất của Legion, performance đỉnh cao", "Intel Core i9 Gen 13 | RTX 4090 | RAM 32GB | SSD 1TB", 10, 14),
                    ("GIGABYTE Aorus 15 - RTX 4060", 18990000m, 22990000m, "Laptop Gaming", "GIGABYTE", "Laptop gaming entry-level, giá tốt cho người mới", "Intel Core i5 Gen 12 | RTX 4060 | RAM 16GB | SSD 512GB", 11, 17),
                    ("MSI Raider GE78 HX - RTX 4070 Ti", 39990000m, 45990000m, "Laptop Gaming", "MSI", "Màn hình 240Hz, chip tân trang nhất", "Intel Core i7 Gen 13 | RTX 4070 Ti | RAM 32GB | SSD 1TB", 12, 13),

                    // ===== LINH KIỆN: MAIN, CPU, VGA (3 sản phẩm) =====
                    ("ASUS ROG STRIX Z890-E - Intel Socket LGA1700", 7990000m, 9490000m, "Main, CPU, VGA", "ASUS", "Bo mạch chủ gaming cao cấp cho Socket LGA1700, hỗ trợ DDR5", "LGA1700 | DDR5 | PCIe 5.0 | WiFi 7", 13, 16),
                    ("Intel Core i9-14900KS - 24 Cores", 16990000m, 19990000m, "Main, CPU, VGA", "INTEL", "CPU gaming flagship, 24 cores/32 threads, hiệu năng đỉnh cao", "LGA1700 | 24 cores | 6.2GHz Turbo | TDP 150W", 14, 15),
                    ("NVIDIA RTX 5090 - 32GB GDDR7", 89990000m, 99990000m, "Main, CPU, VGA", "NVIDIA", "GPU gaming quái vật, VRAM 32GB GDDR7, hiệu năng cực đỉnh", "32GB GDDR7 | PCIe 5.0 | 575W TDP | 21760 CUDA cores", 15, 10),

                    // ===== PC GAMING (6 sản phẩm) =====
                    ("PC Gaming ASUS - RTX 3070 Ti", 29990000m, 35990000m, "PC GVN", "ASUS", "PC gaming cấu hình cao, chơi game AAA siêu mượt", "Intel Core i7-12700K | RTX 3070 Ti | RAM 32GB | SSD 1TB NVMe", 16, 17),
                    ("MSI Aegis RS - RTX 4090 Ultra", 52990000m, 62990000m, "PC GVN", "MSI", "PC gaming siêu cao cấp, cấu hình bá đạo", "Intel Core i9-13900K | RTX 4090 | RAM 64GB | SSD 2TB NVMe", 17, 16),
                    ("NZXT BLD - RTX 2060 Basic", 12990000m, 15990000m, "PC GVN", "NZXT", "PC gaming cơ bản, phù hợp cho gaming casual", "AMD Ryzen 5 5600X | RTX 2060 | RAM 16GB | SSD 500GB NVMe", 18, 19),
                    ("Corsair Graphite - RTX 3080 Pro", 35990000m, 42990000m, "PC GVN", "CORSAIR", "Case cao cấp Corsair, cấu hình chuyên game", "Intel Core i7-12700 | RTX 3080 | RAM 32GB | SSD 1TB NVMe", 19, 16),
                    ("Lian Li Lancool - RTX 4070 Pro", 31990000m, 38990000m, "PC GVN", "LIAN LI", "Case Lancool siêu đẹp, hiệu năng ổn định", "Intel Core i5-12600K | RTX 4070 | RAM 32GB | SSD 1TB NVMe", 20, 18),
                    ("GIGABYTE AORUS - RTX 3090 Ti Monster", 54990000m, 64990000m, "PC GVN", "GIGABYTE", "PC quái vật, RTX 3090 Ti cực khủng", "Intel Core i9-12900K | RTX 3090 Ti | RAM 64GB | SSD 2TB NVMe", 21, 15),

                    // ===== CHUỘT (4 sản phẩm) =====
                    ("Logitech G Pro X Superlight 2", 2490000m, 2990000m, "Chuột + Lót chuột", "LOGITECH", "Chuột gaming siêu nhẹ, 1ms latency, đẳng cấp esports", "Wireless | 25600 DPI | 2.2g | Sạc nhanh", 19, 17),
                    ("Razer Viper V3 Pro", 2990000m, 3490000m, "Chuột + Lót chuột", "RAZER", "Chuột siêu nhạy, sensor PixelFocus HD", "Wireless | 30000 DPI | 2.54g | RogueFlow switch", 20, 14),
                    ("SteelSeries Rival 5", 1990000m, 2490000m, "Chuột + Lót chuột", "STEELSERIES", "Chuột công thái học, phù hợp dùng lâu", "Wired | 18000 DPI | Tùy chỉnh trọng lượng", 21, 20),
                    ("Corsair Sabre RGB Pro", 1490000m, 1890000m, "Chuột + Lót chuột", "CORSAIR", "Chuột chất lượng tốt giá hợp lý", "Wired | 18000 DPI | RGB Corsair Link", 22, 21),

                    // ===== BÀN PHÍM (4 sản phẩm) =====
                    ("AKKO MOD007 Bi-Color", 2990000m, 3490000m, "Bàn phím", "AKKO", "Bàn phím cơ hot-swap, tùy chỉnh full", "Gasket Mount | Holy Panda V4 | RGB Per-Key | Wireless", 23, 14),
                    ("Corsair K100 RGB", 5490000m, 6490000m, "Bàn phím", "CORSAIR", "Bàn phím cao cấp, tổng hợp khâu", "Corsair Mecha switches | RGB Per-Key | Aluminum | Wired", 24, 15),
                    ("Durgod Hades 68", 1890000m, 2290000m, "Bàn phím", "DURGOD", "Bàn phím gaming giá tốt, 68 phím compact", "Outemu Blue | RGB | Aluminum Case | USB", 25, 18),
                    ("Keychron Q1 Pro", 2890000m, 3390000m, "Bàn phím", "KEYCHRON", "Bàn phím 75%, wireless + wired", "Mechanical switches | RGB | Aluminum | Bluetooth", 26, 15),

                    // ===== TAI NGHE (4 sản phẩm) =====
                    ("HyperX Cloud Stinger 2", 1490000m, 1890000m, "Tai nghe", "HYPERX", "Tai nghe gaming giá rẻ mà tốt, sound cân bằng", "7.1 Virtual Surround | Noise Cancellation | 10m Wireless", 27, 21),
                    ("Corsair HS80 RGB", 2490000m, 2990000m, "Tai nghe", "CORSAIR", "Tai nghe gaming cao cấp, thoải mái dùng lâu", "Wireless | 20Hz-20kHz | Surround Sound | RGB", 28, 17),
                    ("Razer Kraken V3", 2290000m, 2690000m, "Tai nghe", "RAZER", "Tai nghe tạo âm thanh 360, immersive", "Wired | Razer Surround | RGB | Chất lượng âm tốt", 29, 15),
                    ("SteelSeries Arctis 9", 3490000m, 3990000m, "Tai nghe", "STEELSERIES", "Tai nghe wireless siêu nhạy, đèn LED rgb đẹp", "Wireless | 2.4GHz | Mic noise-canceling | RGB", 30, 13),

                    // ===== MÀN HÌNH (4 sản phẩm) =====
                    ("LG UltraGear 27\" 1440p 144Hz", 8490000m, 9990000m, "Màn hình", "LG", "Màn hình 1440p 144Hz IPS, tuyệt đẹp", "27 inch | 1440p | 144Hz | IPS | HDR400", 31, 15),
                    ("ASUS ROG Swift 360Hz", 14990000m, 17990000m, "Màn hình", "ASUS", "Màn hình 1440p 360Hz, đỉnh cao esports", "27 inch | 1440p | 360Hz | IPS | NVIDIA G-Sync", 32, 17),
                    ("Dell S3422DWG Ultrawide", 12490000m, 14990000m, "Màn hình", "DELL", "Màn hình ultrawide 34\", công thái học tuyệt vời", "34 inch | 3440x1440 | 144Hz | IPS | Curved", 33, 17),
                    ("LG 27UP550 4K", 9990000m, 11990000m, "Màn hình", "LG", "Màn hình 4K 60Hz, màu sắc chính xác 98% DCI-P3", "27 inch | 4K | 60Hz | IPS | USB-C", 34, 17),

                    // ===== PHỤ KIỆN (6 sản phẩm) =====
                    ("Lót Chuột SteelSeries QcK Prism", 890000m, 1190000m, "Loa, Micro, Webcam", "STEELSERIES", "Lót chuột gaming XL 900x300mm, chất lượng cao", "900x300x4mm | Cloth | RGB Prism | Anti-slip", 35, 25),
                    ("HyperX Alloy Elite RGB", 2290000m, 2790000m, "Bàn phím", "HYPERX", "Bàn phím cơ chất lượng tốt giá rẻ", "Mechanical | Cherry MX | RGB | Aluminum", 36, 18),
                    ("Corsair MM1000 Qi", 1990000m, 2490000m, "Loa, Micro, Webcam", "CORSAIR", "Đế sạc không dây kiêm lót chuột, tiện lợi", "Wireless Charging | 350x250mm | RGB", 37, 20),
                    ("NZXT Camflow", 1290000m, 1590000m, "Loa, Micro, Webcam", "NZXT", "Webcam 1080p 60fps, tuyệt vời cho stream", "1080p | 60fps | Auto-focus | Built-in mic", 38, 19),
                    ("Elgato Wave:3", 2190000m, 2590000m, "Loa, Micro, Webcam", "ELGATO", "Microphone USB chất lượng cao cho streamer", "Wave cancellation | Tap-to-Mute | Auto-gain", 39, 15),
                    ("AmazonBasics USB Hub 3.0 7-Port", 590000m, 790000m, "Loa, Micro, Webcam", "AMAZON", "Hub USB 3.0 7 cổng, cấp điện riêng", "7 ports USB 3.0 | 5Gbps | Power Adapter 12V/4A", 40, 25),

                    // ===== LINH KIỆN (4 sản phẩm) =====
                    ("Kingston Fury Beast 32GB DDR5", 3490000m, 4290000m, "Ổ cứng, RAM, Thẻ nhớ", "KINGSTON", "RAM DDR5 32GB cho gaming/workstation hiệu năng cao", "32GB | 6000MHz | CAS 30 | Kingston Fury", 41, 19),
                    ("Samsung 990 Pro 2TB NVMe", 4990000m, 5990000m, "Ổ cứng, RAM, Thẻ nhớ", "SAMSUNG", "SSD NVMe PCIe 4.0 siêu nhanh, đỉnh cao", "2TB | 7100MB/s | PCIe 4.0 | Gen 4", 42, 17),
                    ("Corsair RM1000e 1000W", 4290000m, 5290000m, "Case, Nguồn, Tản", "CORSAIR", "Nguồn 1000W 80+ Gold, hiệu suất cao", "1000W | 80+ Gold | 135mm fan | Modular", 43, 19),
                    ("Noctua NH-U12A CPU Cooler", 1790000m, 2190000m, "Case, Nguồn, Tản", "NOCTUA", "Tản nhiệt không khí cao cấp, yên tĩnh", "120mm | LGA1700/AM5 | 5.5 TDP max 250W", 44, 18)
                };

                // Lấy brand và category từ DB
                var brandMap = brands.ToDictionary(b => b.TenHangSX, b => b.MaHangSX);
                var categoryMap = categories.ToDictionary(c => c.TenDanhMuc, c => c.MaDanhMuc);

                foreach (var prod in products)
                {
                    if (!brandMap.TryGetValue(prod.brand, out var brandId) || 
                        !categoryMap.TryGetValue(prod.categoryName, out var catId))
                        continue;

                    context.SanPhams.Add(new SanPham
                    {
                        TenSanPham = prod.name,
                        Gia = prod.price,
                        GiaNiemYet = prod.msrp,
                        MaDanhMuc = catId,
                        MaHangSX = brandId,
                        TrangThaiSanPham = 1,
                        ThoiGianTaoSP = DateTime.Now.AddDays(-random.Next(1, 60)),
                        KhuyenMai = prod.discount > 0 ? $"Giảm {prod.discount}% hôm nay|Tặng Balo Gaming HDK|Vệ sinh máy miễn phí" : "Tặng Balo Gaming HDK|Vệ sinh máy miễn phí",
                        ThongTinBaoHanh = "24 Tháng chính hãng",
                        MoTaSanPham = $"<h4 class='text-danger'>Đặc điểm nổi bật</h4><ul><li>{prod.description}</li><li>Bảo hành chính hãng 24 tháng từ HDKTech</li><li>Giao hàng toàn quốc trong 24-48 giờ</li></ul>",
                        ThongSoKyThuat = prod.specs
                    });
                }
                await context.SaveChangesAsync();

                // Thêm ảnh và kho hàng cho các sản phẩm
                // Mapping ảnh theo loại sản phẩm để sử dụng placeholders đã tạo
                var imageMapping = new Dictionary<int, string>
                {
                    // Ultrabooks (ID 1-6) -> laptops
                    { 1, "laptops/dell-xps-13-silver-front.jpg" },
                    { 2, "laptops/asus-vivobook-14-blue-side.jpg" },
                    { 3, "laptops/lenovo-thinkpad-x1-black-angle.jpg" },
                    { 4, "laptops/macbook-air-m3-silver-front.jpg" },
                    { 5, "laptops/hp-pavilion-15-white-front.jpg" },
                    { 6, "laptops/acer-aspire-5-red-angle.jpg" },

                    // Gaming Laptops (ID 7-12) -> laptops-gaming
                    { 7, "laptops-gaming/asus-rog-strix-black-angle.jpg" },
                    { 8, "laptops-gaming/msi-katana-red-black-angle.jpg" },
                    { 9, "laptops-gaming/acer-predator-dark-side.jpg" },
                    { 10, "laptops-gaming/lenovo-legion-9-pro-angle.jpg" },
                    { 11, "laptops-gaming/gigabyte-aorus-dark-gray-front.jpg" },
                    { 12, "laptops-gaming/msi-raider-ge78-angle.jpg" },

                    // Gaming PCs (ID 13-18) -> pc-builds
                    { 13, "pc-builds/asus-pc-gaming-high-end-front.jpg" },
                    { 14, "pc-builds/msi-aegis-rs-black-angle.jpg" },
                    { 15, "pc-builds/nzxt-bld-white-front.jpg" },
                    { 16, "pc-builds/corsair-graphite-dark-gray-angle.jpg" },
                    { 17, "pc-builds/lian-li-lancool-rgb-front.jpg" },
                    { 18, "pc-builds/gigabyte-aorus-monster-angle.jpg" },

                    // Mice (ID 19-22) -> peripherals
                    { 19, "peripherals/logitech-g-pro-x-black-top.jpg" },
                    { 20, "peripherals/razer-viper-v3-black-angle.jpg" },
                    { 21, "peripherals/steelseries-rival-5-dark-gray-side.jpg" },
                    { 22, "peripherals/corsair-sabre-rgb-black-front.jpg" },

                    // Keyboards (ID 23-26) -> peripherals
                    { 23, "peripherals/akko-mod007-white-angle.jpg" },
                    { 24, "peripherals/corsair-k100-rgb-black-angle.jpg" },
                    { 25, "peripherals/durgod-hades-68-black-side.jpg" },
                    { 26, "peripherals/keychron-q1-pro-gray-angle.jpg" },

                    // Headsets (ID 27-30) -> audio
                    { 27, "audio/hyperx-cloud-stinger-2-black-side.jpg" },
                    { 28, "audio/corsair-hs80-rgb-dark-angle.jpg" },
                    { 29, "audio/razer-kraken-v3-black-front.jpg" },
                    { 30, "audio/steelseries-arctis-9-gray-angle.jpg" },

                    // Monitors (ID 31-34) -> monitor
                    { 31, "monitor/lg-ultragear-27-1440p-front.jpg" },
                    { 32, "monitor/asus-rog-swift-360hz-black-angle.jpg" },
                    { 33, "monitor/dell-s3422dwg-ultrawide-black-front.jpg" },
                    { 34, "monitor/lg-27up550-4k-white-front.jpg" },

                    // Accessories (ID 35-40) -> accessories
                    { 35, "accessories/steelseries-qck-prism-mat-black-top.jpg" },
                    { 36, "accessories/hyperx-alloy-elite-rgb-angle.jpg" },
                    { 37, "accessories/corsair-mm1000-qi-charging-top.jpg" },
                    { 38, "accessories/nzxt-camflow-1080p-webcam-front.jpg" },
                    { 39, "accessories/elgato-wave-3-microphone-side.jpg" },
                    { 40, "accessories/amazon-basics-usb-hub-front.jpg" },

                    // Components (ID 41-44) -> storage/accessories
                    { 41, "storage/ram-kingston-fury-box.jpg" },
                    { 42, "storage/ssd-samsung-990-pro-box.jpg" },
                    { 43, "accessories/psu-corsair-rm1000e-front.jpg" },
                    { 44, "accessories/cooler-corsair-h150i-front.jpg" }
                };

                var allProds = await context.SanPhams.ToListAsync();
                int imageCounter = 1;
                foreach (var p in allProds)
                {
                    string imageUrl = imageMapping.ContainsKey(imageCounter) 
                        ? imageMapping[imageCounter] 
                        : $"products/placeholder-{imageCounter}.jpg";

                    context.HinhAnhs.Add(new HinhAnh { MaSanPham = p.MaSanPham, Url = imageUrl, IsDefault = true });
                    context.KhoHangs.Add(new KhoHang { MaSanPham = p.MaSanPham, SoLuong = random.Next(10, 100), NgayCapNhat = DateTime.Now });
                    imageCounter++;
                }
                await context.SaveChangesAsync();
            }
            #endregion

            #region 5. ĐÁNH GIÁ (REVIEWS - Dữ liệu thật cho Tab Review)
            if (!await context.DanhGias.AnyAsync())
            {
                var allProducts = await context.SanPhams.Take(20).ToListAsync();
                var users = await context.Users.ToListAsync();

                var reviewComments = new[] {
        "Máy chạy cực nhanh, thiết kế đẹp đẳng cấp. Rất đáng đồng tiền bát gạo!",
        "Giao hàng hỏa tốc, đóng gói rất kỹ. Nhân viên HDKTech tư vấn nhiệt tình.",
        "Sản phẩm chuẩn chính hãng, đã check serial. Sẽ ủng hộ shop dài dài.",
        "Cấu hình mạnh chơi game rất mượt, tản nhiệt tốt không bị nóng máy.",
        "Màn hình sắc nét, màu sắc chuẩn xác cho dân đồ họa như mình."
    };

                foreach (var p in allProducts)
                {
                    int reviewCount = random.Next(2, 4);
                    for (int i = 0; i < reviewCount; i++)
                    {
                        var user = users[random.Next(users.Count)];
                        context.DanhGias.Add(new DanhGia
                        {
                            MaSanPham = p.MaSanPham,
                            IdNguoiDung = user.Id,
                            NoiDung = reviewComments[random.Next(reviewComments.Length)],
                            SoSao = random.Next(4, 6),
                            NgayDanhGia = DateTime.Now.AddDays(-random.Next(1, 30))
                        });
                    }
                }
                await context.SaveChangesAsync();
            }
            #endregion

            #region 6. THÊM ẢNH BANNER CHO CÁC DANH MỤC
            await SeedBannerImages(context);
            #endregion
        }

        private static async Task SeedBannerImages(HDKTechContext context)
        {
            // Danh sách ảnh CHÍNH THỨC từ ProductSeeds.json - ánh xạ MaSanPham → ảnh
            var bannerImages = new Dictionary<int, List<(string url, bool isDefault)>>
            {
                // Laptop (1-6)
                { 1, new List<(string, bool)> { ("laptops/dell-xps-13-silver-front.jpg", true), ("laptops/dell-xps-13-silver-side.jpg", false), ("laptops/dell-xps-13-silver-keyboard.jpg", false) } },
                { 2, new List<(string, bool)> { ("laptops/asus-vivobook-14x-silver.jpg", true), ("laptops/asus-vivobook-14x-open.jpg", false) } },
                { 3, new List<(string, bool)> { ("laptops/lenovo-thinkpad-x1-black.jpg", true), ("laptops/lenovo-thinkpad-x1-keyboard.jpg", false) } },
                { 4, new List<(string, bool)> { ("laptops/macbook-air-m3-space-gray.jpg", true), ("laptops/macbook-air-m3-silver.jpg", false), ("laptops/macbook-air-m3-keyboard.jpg", false) } },
                { 5, new List<(string, bool)> { ("laptops/hp-pavilion-15-blue.jpg", true), ("laptops/hp-pavilion-15-open.jpg", false) } },
                { 6, new List<(string, bool)> { ("laptops/acer-aspire-5-silver.jpg", true), ("laptops/acer-aspire-5-open.jpg", false) } },

                // Laptop Gaming (7-10)
                { 7, new List<(string, bool)> { ("laptops-gaming/asus-rog-strix-g16-black.jpg", true), ("laptops-gaming/asus-rog-strix-g16-open.jpg", false), ("laptops-gaming/asus-rog-strix-g16-rgb.jpg", false) } },
                { 8, new List<(string, bool)> { ("laptops-gaming/msi-katana-17-black.jpg", true), ("laptops-gaming/msi-katana-17-open.jpg", false) } },
                { 9, new List<(string, bool)> { ("laptops-gaming/acer-predator-18-black.jpg", true), ("laptops-gaming/acer-predator-18-open.jpg", false), ("laptops-gaming/acer-predator-18-rgb.jpg", false) } },
                { 10, new List<(string, bool)> { ("laptops-gaming/lenovo-legion-9-pro-black.jpg", true), ("laptops-gaming/lenovo-legion-9-pro-open.jpg", false) } },

                // PC GVN (11-13)
                { 11, new List<(string, bool)> { ("pc-builds/gaming-rtx-4060-front.jpg", true), ("pc-builds/gaming-rtx-4060-open.jpg", false), ("pc-builds/gaming-rtx-4060-interior.jpg", false) } },
                { 12, new List<(string, bool)> { ("pc-builds/gaming-rtx-5080-front.jpg", true), ("pc-builds/gaming-rtx-5080-open.jpg", false), ("pc-builds/gaming-rtx-5080-interior.jpg", false) } },
                { 13, new List<(string, bool)> { ("pc-builds/gaming-rtx-5090-front.jpg", true), ("pc-builds/gaming-rtx-5090-open.jpg", false), ("pc-builds/gaming-rtx-5090-interior.jpg", false) } },

                // Main, CPU, VGA (14-17)
                { 14, new List<(string, bool)> { ("components/mainboard-asus-rog-z890-front.jpg", true), ("components/mainboard-asus-rog-z890-back.jpg", false) } },
                { 15, new List<(string, bool)> { ("components/cpu-intel-i9-14900k-box.jpg", true), ("components/cpu-intel-i9-14900k-chip.jpg", false) } },
                { 16, new List<(string, bool)> { ("components/gpu-rtx-5090-front.jpg", true), ("components/gpu-rtx-5090-side.jpg", false), ("components/gpu-rtx-5090-back.jpg", false) } },
                { 17, new List<(string, bool)> { ("components/gpu-rtx-5080-front.jpg", true), ("components/gpu-rtx-5080-side.jpg", false) } },

                // Case, Nguồn, Tản (18-21)
                { 18, new List<(string, bool)> { ("accessories/case-corsair-570x-front.jpg", true), ("accessories/case-corsair-570x-open.jpg", false), ("accessories/case-corsair-570x-interior.jpg", false) } },
                { 19, new List<(string, bool)> { ("accessories/case-lian-li-lancool3-black.jpg", true), ("accessories/case-lian-li-lancool3-open.jpg", false) } },
                { 20, new List<(string, bool)> { ("accessories/psu-corsair-rm1000e-front.jpg", true), ("accessories/psu-corsair-rm1000e-cable.jpg", false) } },
                { 21, new List<(string, bool)> { ("accessories/cooler-corsair-h150i-front.jpg", true), ("accessories/cooler-corsair-h150i-mounted.jpg", false), ("accessories/cooler-corsair-h150i-rad.jpg", false) } },

                // Ổ cứng, RAM (22-24)
                { 22, new List<(string, bool)> { ("storage/ssd-samsung-990-pro-box.jpg", true), ("storage/ssd-samsung-990-pro-drive.jpg", false) } },
                { 23, new List<(string, bool)> { ("storage/ram-corsair-dominator-box.jpg", true), ("storage/ram-corsair-dominator-stick.jpg", false) } },
                { 24, new List<(string, bool)> { ("storage/ram-kingston-fury-box.jpg", true), ("storage/ram-kingston-fury-stick.jpg", false) } },

                // Loa, Micro, Webcam (25-26)
                { 25, new List<(string, bool)> { ("audio/speaker-edifier-s1000db-front.jpg", true), ("audio/speaker-edifier-s1000db-pair.jpg", false) } },
                { 26, new List<(string, bool)> { ("audio/webcam-logitech-brio-4k-front.jpg", true), ("audio/webcam-logitech-brio-4k-stand.jpg", false) } },

                // Màn hình (27-29)
                { 27, new List<(string, bool)> { ("monitor/asus-pa347cv-front.jpg", true), ("monitor/asus-pa347cv-stand.jpg", false) } },
                { 28, new List<(string, bool)> { ("monitor/lg-27gp850-front.jpg", true), ("monitor/lg-27gp850-stand.jpg", false) } },
                { 29, new List<(string, bool)> { ("monitor/asus-proart-32-front.jpg", true), ("monitor/asus-proart-32-stand.jpg", false) } },

                // Bàn phím (30-31)
                { 30, new List<(string, bool)> { ("peripherals/keyboard-akko-3098b-front.jpg", true), ("peripherals/keyboard-akko-3098b-switches.jpg", false), ("peripherals/keyboard-akko-3098b-keycaps.jpg", false) } },
                { 31, new List<(string, bool)> { ("peripherals/keyboard-corsair-k95-front.jpg", true), ("peripherals/keyboard-corsair-k95-rgb.jpg", false) } },

                // Chuột + Lót chuột (32-34)
                { 32, new List<(string, bool)> { ("peripherals/mouse-logitech-mx-master-front.jpg", true), ("peripherals/mouse-logitech-mx-master-side.jpg", false) } },
                { 33, new List<(string, bool)> { ("peripherals/mouse-razer-deathadder-v3-front.jpg", true), ("peripherals/mouse-razer-deathadder-v3-side.jpg", false) } },
                { 34, new List<(string, bool)> { ("peripherals/mousepad-steelseries-qck-front.jpg", true) } },

                // Tai nghe (35-37)
                { 35, new List<(string, bool)> { ("peripherals/headset-hyperx-cloud-front.jpg", true), ("peripherals/headset-hyperx-cloud-side.jpg", false) } },
                { 36, new List<(string, bool)> { ("peripherals/headset-corsair-virtuoso-front.jpg", true), ("peripherals/headset-corsair-virtuoso-side.jpg", false) } },
                { 37, new List<(string, bool)> { ("peripherals/headset-razer-blackshark-front.jpg", true), ("peripherals/headset-razer-blackshark-side.jpg", false) } },

                // Ghế - Bàn (38-40)
                { 38, new List<(string, bool)> { ("furniture/desk-autonomous-smartdesk-front.jpg", true), ("furniture/desk-autonomous-smartdesk-setup.jpg", false) } },
                { 39, new List<(string, bool)> { ("furniture/chair-herman-miller-aeron-front.jpg", true), ("furniture/chair-herman-miller-aeron-side.jpg", false) } },
                { 40, new List<(string, bool)> { ("furniture/chair-secretlab-titan-front.jpg", true), ("furniture/chair-secretlab-titan-side.jpg", false), ("furniture/chair-secretlab-titan-recline.jpg", false) } },

                // Handheld, Console (41-43)
                { 41, new List<(string, bool)> { ("handheld/asus-rog-ally-x-front.jpg", true), ("handheld/asus-rog-ally-x-screen.jpg", false) } },
                { 42, new List<(string, bool)> { ("handheld/ps5-slim-front.jpg", true), ("handheld/ps5-slim-controller.jpg", false) } },
                { 43, new List<(string, bool)> { ("handheld/switch-oled-front.jpg", true), ("handheld/switch-oled-dock.jpg", false) } },

                // Dịch vụ (44-45)
                { 44, new List<(string, bool)> { ("services/service-build-pc-icon.jpg", true) } },
                { 45, new List<(string, bool)> { ("services/service-repair-icon.jpg", true) } },
            };

            // Bước 1: XÓA tất cả hình ảnh cũ (generic như "1_1.jpg", "2_1.jpg")
            var productsToUpdate = bannerImages.Keys.ToList();
            var oldImages = await context.HinhAnhs
                .Where(h => productsToUpdate.Contains(h.MaSanPham))
                .ToListAsync();

            foreach (var oldImg in oldImages)
            {
                context.HinhAnhs.Remove(oldImg);
            }
            await context.SaveChangesAsync();

            // Bước 2: THÊM hình ảnh mới
            foreach (var productImages in bannerImages)
            {
                var productExists = await context.SanPhams.AnyAsync(p => p.MaSanPham == productImages.Key);
                if (!productExists)
                    continue;

                foreach (var image in productImages.Value)
                {
                    context.HinhAnhs.Add(new HinhAnh
                    {
                        MaSanPham = productImages.Key,
                        Url = image.url,
                        IsDefault = image.isDefault,
                        AltText = image.url.Split('/').LastOrDefault()?.Replace(".jpg", ""),
                        NgayTao = DateTime.Now
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedSub(HDKTechContext context, DanhMuc parent, string subName, string[] grandChildren)
        {
            var sub = new DanhMuc { TenDanhMuc = subName, MaDanhMucCha = parent.MaDanhMuc };
            context.DanhMucs.Add(sub);
            await context.SaveChangesAsync();
            foreach (var gc in grandChildren)
                context.DanhMucs.Add(new DanhMuc { TenDanhMuc = gc, MaDanhMucCha = sub.MaDanhMuc });
            await context.SaveChangesAsync();
        }
    }
}

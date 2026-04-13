using Bogus;
using HDKTech.ChucNangPhanQuyen;
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HDKTech.Areas.Identity.Data
{
    /// <summary>
    /// Chỉ chứa logic seed SẢN PHẨM với Specifications chuẩn
    /// </summary>
    public static class DataSeedProducts
    {
        public static async Task SeedProductsWithSpecs(HDKTechContext context)
        {
            if (await context.Products.AnyAsync())
                return;

            var brands = await context.Brands.ToListAsync();
            var categories = await context.Categories.Where(c => c.ParentCategoryId == null).ToListAsync();
            var random = new Random();

            // 44 SẢN PHẨM THỰC TẾ với Specifications định dạng "Key: Value | Key: Value"
            var products = new List<(string name, decimal price, decimal? msrp, string categoryName, string brand, string description, string specs, string imageFolder, string imageFile, int discount)>
            {
                // ===== LAPTOP ULTRABOOK (6 sản phẩm) =====
                ("Dell XPS 13 Plus - Intel Core i5 Gen 13", 24990000m, 27990000m, "Laptop", "DELL", 
                    "Thiết kế siêu mỏng, hiệu năng mạnh mẽ cho công việc văn phòng. Màn hình 13.4\" InfinityEdge, bàn phím công thái học.",
                    "CPU: Intel Core i5-1340P | RAM: 8GB LPDDR5 | SSD: 512GB NVMe | GPU: Intel Iris Xe | Màn hình: 13.4\" FHD InfinityEdge | Pin: 55Wh",
                    "laptops", "dell-xps-13-silver-front.jpg", 11),

                ("ASUS VivoBook 14 - AMD Ryzen 5 5500U", 11990000m, 13990000m, "Laptop", "ASUS",
                    "Máy tính xách tay nhẹ nhàng, thích hợp cho du lịch. Trọng lượng chỉ 1.4kg, kích thước compact.",
                    "CPU: AMD Ryzen 5 5500U | RAM: 8GB LPDDR4X | SSD: 512GB SSD PCIe | GPU: Radeon Graphics | Màn hình: 14\" IPS FHD | Trọng lượng: 1.4kg",
                    "laptops", "asus-vivobook-14-blue-side.jpg", 14),

                ("Lenovo ThinkPad X1 Carbon - Intel Core i7", 32990000m, 36990000m, "Laptop", "LENOVO",
                    "Dòng máy cao cấp cho doanh nhân chuyên nghiệp. Bảo mật vân tay, camera IR.",
                    "CPU: Intel Core i7-1280P | RAM: 16GB LPDDR5 | SSD: 512GB SSD PCIe Gen 4 | GPU: Intel Iris Xe | Màn hình: 14\" 2.8K OLED | Pin: 52Wh",
                    "laptops", "lenovo-thinkpad-x1-black-angle.jpg", 11),

                ("MacBook Air M3 - 2024", 28990000m, null, "Laptop", "APPLE",
                    "Chip Apple M3 mạnh mẽ, thời lượng pin vô địch. Máy chạy macOS Sonoma mới nhất.",
                    "CPU: Apple M3 (8-core) | GPU: Apple M3 (8-core) | RAM: 8GB Unified Memory | SSD: 256GB | Màn hình: 13.6\" Liquid Retina | Pin: 52.6Wh",
                    "laptops", "macbook-air-m3-silver-front.jpg", 0),

                ("HP Pavilion 15 - Intel Core i5", 12990000m, 15290000m, "Laptop", "HP",
                    "Laptop giá rẻ, hiệu năng ổn định cho học tập. Màn hình 15.6\" rộng rãi.",
                    "CPU: Intel Core i5-1235U | RAM: 8GB DDR4 | SSD: 256GB SSD | GPU: Intel UHD | Màn hình: 15.6\" FHD IPS | Pin: 41.2Wh",
                    "laptops", "hp-pavilion-15-white-front.jpg", 15),

                ("Acer Aspire 5 - AMD Ryzen 7", 14990000m, 17990000m, "Laptop", "ACER",
                    "Màn hình 15.6 inch IPS, hiệu năng tốt giá cạnh tranh. Hệ thống tản nhiệt hiệu quả.",
                    "CPU: AMD Ryzen 7 5700U | RAM: 16GB DDR4 | SSD: 512GB SSD | GPU: Radeon Graphics | Màn hình: 15.6\" FHD IPS | Pin: 50Wh",
                    "laptops", "acer-aspire-5-red-angle.jpg", 17),

                // ===== LAPTOP GAMING (6 sản phẩm) =====
                ("ASUS ROG Strix G16 - RTX 4090", 49990000m, 59990000m, "Laptop Gaming", "ASUS",
                    "Laptop gaming flagship, GPU RTX 4090 quái vật. Màn hình 240Hz siêu nhanh.",
                    "CPU: Intel Core i9-13900HX | GPU: RTX 4090 Laptop (16GB GDDR6) | RAM: 32GB DDR5 | SSD: 1TB NVMe PCIe 4.0 | Màn hình: 16\" 2560x1600 240Hz IPS | Pin: 90Wh",
                    "laptops-gaming", "asus-rog-strix-black-angle.jpg", 17),

                ("MSI Katana 17 - RTX 4070", 27990000m, 32990000m, "Laptop Gaming", "MSI",
                    "Màn hình 144Hz IPS, hiệu năng gaming mạnh. Chip tân trang nhất từ Intel.",
                    "CPU: Intel Core i7-13700H | GPU: RTX 4070 Laptop (8GB GDDR6) | RAM: 16GB DDR5 | SSD: 512GB NVMe PCIe 4.0 | Màn hình: 17\" FHD 144Hz IPS | Pin: 90Wh",
                    "laptops-gaming", "msi-katana-red-black-angle.jpg", 15),

                ("Acer Predator 18 - RTX 4080", 42990000m, 49990000m, "Laptop Gaming", "ACER",
                    "Màn hình 18 inch khổng lồ, RTX 4080 siêu mạnh. Dành cho những game thủ muốn trải nghiệm cực đại.",
                    "CPU: Intel Core i9-13900HX | GPU: RTX 4080 Laptop (12GB GDDR6) | RAM: 32GB DDR5 | SSD: 1TB NVMe PCIe 4.0 | Màn hình: 18\" 2560x1600 240Hz IPS | Pin: 99Wh",
                    "laptops-gaming", "acer-predator-dark-side.jpg", 14),

                ("Lenovo Legion 9 Pro - RTX 4090 Laptop", 59990000m, 69990000m, "Laptop Gaming", "LENOVO",
                    "Dòng cao cấp nhất của Legion, performance đỉnh cao. Hệ thống làm mát Vapor Chamber tiên tiến.",
                    "CPU: Intel Core i9-13980HX | GPU: RTX 4090 Laptop (16GB GDDR6) | RAM: 32GB LPDDR5X | SSD: 2TB SSD NVMe | Màn hình: 16\" 2560x1600 240Hz OLED | Pin: 99Wh",
                    "laptops-gaming", "lenovo-legion-9-pro-angle.jpg", 14),

                ("GIGABYTE Aorus 15 - RTX 4060", 18990000m, 22990000m, "Laptop Gaming", "GIGABYTE",
                    "Laptop gaming entry-level, giá tốt cho người mới. RTX 4060 đủ mạnh cho game 1440p.",
                    "CPU: Intel Core i5-12500H | GPU: RTX 4060 Laptop (6GB GDDR6) | RAM: 16GB DDR5 | SSD: 512GB SSD | Màn hình: 15.6\" 2560x1440 165Hz IPS | Pin: 65Wh",
                    "laptops-gaming", "gigabyte-aorus-dark-gray-front.jpg", 17),

                ("MSI Raider GE78 HX - RTX 4070 Ti", 39990000m, 45990000m, "Laptop Gaming", "MSI",
                    "Màn hình 240Hz, chip tân trang nhất. Thiết kế RGB siêu đẹp mắt.",
                    "CPU: Intel Core i7-13700HX | GPU: RTX 4070 Ti Laptop (8GB GDDR6) | RAM: 32GB DDR5 | SSD: 1TB NVMe | Màn hình: 17.3\" FHD 240Hz IPS | Pin: 99Wh",
                    "laptops-gaming", "msi-raider-ge78-angle.jpg", 13),

                // ===== LINH KIỆN: MAIN, CPU, VGA (3 sản phẩm) =====
                ("ASUS ROG STRIX Z890-E - Intel LGA1700", 7990000m, 9490000m, "Main, CPU, VGA", "ASUS",
                    "Bo mạch chủ gaming cao cấp, hỗ trợ CPU Intel Core Ultra tân trang. PCIe 5.0 full.",
                    "Socket: LGA1700 | Chipset: Z890 | RAM: DDR5 6000+ | PCIe: Gen 5.0 x16 | Tích hợp: WiFi 7, 2.5G Ethernet | Nguồn: 14+2+1 Power Stages",
                    "components", "asus-z890-motherboard-angle.jpg", 16),

                ("Intel Core i9-14900KS - 24 Cores", 16990000m, 19990000m, "Main, CPU, VGA", "INTEL",
                    "CPU gaming flagship, 24 cores/32 threads, hiệu năng đỉnh cao. Xung nhịp Turbo 6.2GHz.",
                    "Cores/Threads: 24/32 | Base/Turbo: 3.2/6.2 GHz | Cache: 36MB L3 | TDP: 150W | Socket: LGA1700 | Công nghệ: 7nm Intel 4",
                    "components", "intel-i9-14900ks-angle.jpg", 15),

                ("NVIDIA RTX 5090 - 32GB GDDR7", 89990000m, 99990000m, "Main, CPU, VGA", "NVIDIA",
                    "GPU gaming quái vật, VRAM 32GB GDDR7 siêu nhanh. Performance tối đa 4000+ TFLOPS.",
                    "VRAM: 32GB GDDR7 | Memory Bus: 576-bit | Memory Bandwidth: 1792 GB/s | CUDA Cores: 21760 | TDP: 575W | PCIe: Gen 5.0",
                    "components", "nvidia-rtx-5090-angle.jpg", 10),

                // ===== PC GAMING (6 sản phẩm) =====
                ("PC Gaming ASUS - RTX 3070 Ti", 29990000m, 35990000m, "PC GVN", "ASUS",
                    "PC gaming cấu hình cao, chơi game AAA siêu mượt. Case ASUS ROG Strix sang trọng.",
                    "CPU: Intel Core i7-12700K | GPU: RTX 3070 Ti | RAM: 32GB DDR4 | SSD: 1TB NVMe PCIe 4.0 | PSU: 850W 80+ Gold | Case: ASUS ROG Strix",
                    "pc-builds", "asus-pc-gaming-high-end-front.jpg", 17),

                ("MSI Aegis RS - RTX 4090 Ultra", 52990000m, 62990000m, "PC GVN", "MSI",
                    "PC gaming siêu cao cấp, cấu hình bá đạo. MSI RTX 4090 Gaming X chuyên dụng.",
                    "CPU: Intel Core i9-13900K | GPU: MSI RTX 4090 Gaming X | RAM: 64GB DDR5 | SSD: 2TB NVMe | PSU: 1200W 80+ Titanium | Case: MSI MPG Gungnir 560R",
                    "pc-builds", "msi-aegis-rs-black-angle.jpg", 16),

                ("NZXT BLD - RTX 2060 Basic", 12990000m, 15990000m, "PC GVN", "NZXT",
                    "PC gaming cơ bản, phù hợp cho gaming casual. NZXT H500 Flow tinh tế.",
                    "CPU: AMD Ryzen 5 5600X | GPU: RTX 2060 6GB | RAM: 16GB DDR4 | SSD: 500GB NVMe | PSU: 650W 80+ Bronze | Case: NZXT H500 Flow",
                    "pc-builds", "nzxt-bld-white-front.jpg", 19),

                ("Corsair Graphite - RTX 3080 Pro", 35990000m, 42990000m, "PC GVN", "CORSAIR",
                    "Case cao cấp Corsair, cấu hình chuyên game. Làm mát tối ưu Corsair H150i AIO.",
                    "CPU: Intel Core i7-12700 | GPU: RTX 3080 | RAM: 32GB Corsair Vengeance DDR4 | SSD: 1TB Corsair MP600 | PSU: 1000W Corsair RM1000e | Case: Corsair 5000T RGB",
                    "pc-builds", "corsair-graphite-dark-gray-angle.jpg", 16),

                ("Lian Li Lancool - RTX 4070 Pro", 31990000m, 38990000m, "PC GVN", "LIAN LI",
                    "Case Lancool siêu đẹp, hiệu năng ổn định. Luồng gió tối ưu.",
                    "CPU: Intel Core i5-12600K | GPU: RTX 4070 | RAM: 32GB DDR4 3600MHz | SSD: 1TB Samsung 980 Pro | PSU: 850W Seasonic Focus GX | Case: Lian Li Lancool 215 RGB",
                    "pc-builds", "lian-li-lancool-rgb-front.jpg", 18),

                ("GIGABYTE AORUS - RTX 3090 Ti Monster", 54990000m, 64990000m, "PC GVN", "GIGABYTE",
                    "PC quái vật, RTX 3090 Ti cực khủng. Hiệu năng 4K siêu cao.",
                    "CPU: Intel Core i9-12900K | GPU: GIGABYTE RTX 3090 Ti Eagle OC | RAM: 64GB DDR4 | SSD: 2TB Gigabyte Aorus NVMe Gen4 | PSU: 1200W 80+ Platinum | Case: GIGABYTE AORUS C700G",
                    "pc-builds", "gigabyte-aorus-monster-angle.jpg", 15),

                // ===== CHUỘT (4 sản phẩm) =====
                ("Logitech G Pro X Superlight 2", 2490000m, 2990000m, "Chuột + Lót chuột", "LOGITECH",
                    "Chuột gaming siêu nhẹ, 1ms latency, đẳng cấp esports. Trọng lượng chỉ 2.2g.",
                    "Trọng lượng: 2.2g | DPI: 25,600 | Polling Rate: 8000Hz | Pin: 95 giờ | Kết nối: Wireless Lightspeed",
                    "peripherals", "logitech-g-pro-x-black-top.jpg", 17),

                ("Razer Viper V3 Pro", 2990000m, 3490000m, "Chuột + Lót chuột", "RAZER",
                    "Chuột siêu nhạy, sensor PixelFocus HD. Thiết kế ergonomic hoàn hảo.",
                    "Trọng lượng: 2.54g | DPI: 30,000 | Polling Rate: 8000Hz | Cảm biến: Focus Pro 30K | Kết nối: Wireless HyperSpeed",
                    "peripherals", "razer-viper-v3-black-angle.jpg", 14),

                ("SteelSeries Rival 5", 1990000m, 2490000m, "Chuột + Lót chuột", "STEELSERIES",
                    "Chuột công thái học, phù hợp dùng lâu. Khoa học trọng lượng tùy chỉnh.",
                    "DPI: 18,000 | Polling Rate: 8000Hz | Trọng lượng: Tùy chỉnh 77-98g | Cảm biến: TrueMove Core | Kết nối: Wired USB",
                    "peripherals", "steelseries-rival-5-dark-gray-side.jpg", 20),

                ("Corsair Sabre RGB Pro", 1490000m, 1890000m, "Chuột + Lót chuột", "CORSAIR",
                    "Chuột chất lượng tốt giá hợp lý. RGB Corsair Link tùy chỉnh hoàn toàn.",
                    "DPI: 18,000 | Polling Rate: 8000Hz | Buttons: 6 tùy chỉnh | Cảm biến: PMW3327 | Kết nối: Wired USB | RGB: Per-Button",
                    "peripherals", "corsair-sabre-rgb-black-front.jpg", 21),

                // ===== BÀN PHÍM (4 sản phẩm) =====
                ("AKKO MOD007 Bi-Color", 2990000m, 3490000m, "Bàn phím", "AKKO",
                    "Bàn phím cơ hot-swap, tùy chỉnh full. Gasket Mount thiết kế tỉ mỉ.",
                    "Layout: 75% | Switch: Holy Panda V4 (Hot-swap) | Keycaps: PBT Double Shot | Kết nối: 2.4GHz Wireless / USB | RGB: Per-Key",
                    "peripherals", "akko-mod007-white-angle.jpg", 14),

                ("Corsair K100 RGB", 5490000m, 6490000m, "Bàn phím", "CORSAIR",
                    "Bàn phím cao cấp, tổng hợp khâu. Switch Corsair Mecha tuyệt vời.",
                    "Layout: Full Size | Switch: Corsair Mecha RGB | Keycaps: PBT Doubleshot | Macro: 15 macro keys | RGB: Per-Key 16M colors",
                    "peripherals", "corsair-k100-rgb-black-angle.jpg", 15),

                ("Durgod Hades 68", 1890000m, 2290000m, "Bàn phím", "DURGOD",
                    "Bàn phím gaming giá tốt, 68 phím compact. Aluminum case chắc chắn.",
                    "Layout: 68 keys | Switch: Outemu Blue | Keycaps: PBT Double Shot | Case: Aluminum | RGB: Per-Key | Kết nối: USB",
                    "peripherals", "durgod-hades-68-black-side.jpg", 18),

                ("Keychron Q1 Pro", 2890000m, 3390000m, "Bàn phím", "KEYCHRON",
                    "Bàn phím 75%, wireless + wired hybrid. Aluminum frame cao cấp.",
                    "Layout: 75% | Switch: Keychron Hot-swap Optical | Kết nối: 2.4GHz + USB wired | RGB: Per-Key Backlit | Pin: 4000mAh",
                    "peripherals", "keychron-q1-pro-gray-angle.jpg", 15),

                // ===== TAI NGHE (4 sản phẩm) =====
                ("HyperX Cloud Stinger 2", 1490000m, 1890000m, "Tai nghe", "HYPERX",
                    "Tai nghe gaming giá rẻ mà tốt, sound cân bằng. Mic noise-canceling tuyệt vời.",
                    "Âm thanh: 7.1 Virtual Surround | Driver: 50mm | Impedance: 32 Ohm | Mic: Removable | Kết nối: 10m Wireless 2.4GHz",
                    "audio", "hyperx-cloud-stinger-2-black-side.jpg", 21),

                ("Corsair HS80 RGB", 2490000m, 2990000m, "Tai nghe", "CORSAIR",
                    "Tai nghe gaming cao cấp, thoải mái dùng lâu. Surround Sound 7.1.",
                    "Âm thanh: 7.1 Surround Sound | Driver: 50mm Neodymium | Impedance: 32 Ohm | Kết nối: Wireless 2.4GHz | Pin: 20 giờ",
                    "audio", "corsair-hs80-rgb-dark-angle.jpg", 17),

                ("Razer Kraken V3", 2290000m, 2690000m, "Tai nghe", "RAZER",
                    "Tai nghe tạo âm thanh 360, immersive. Surround Sound lôi cuốn.",
                    "Âm thanh: Razer Surround 7.1 | Driver: 50mm Triple Force | Mic: Beamforming Mic Boom | Kết nối: Wired USB | RGB: Per-Driver LED",
                    "audio", "razer-kraken-v3-black-front.jpg", 15),

                ("SteelSeries Arctis 9", 3490000m, 3990000m, "Tai nghe", "STEELSERIES",
                    "Tai nghe wireless siêu nhạy, đèn LED rgb đẹp. Mic noise-canceling tuyệt vời.",
                    "Âm thanh: Surround Immersive | Driver: 40mm Neodymium | Kết nối: 2.4GHz Wireless + Bluetooth | Mic: ClearCast mic | Pin: 20 giờ",
                    "audio", "steelseries-arctis-9-gray-angle.jpg", 13),

                // ===== MÀN HÌNH (4 sản phẩm) =====
                ("LG UltraGear 27\" 1440p 144Hz", 8490000m, 9990000m, "Màn hình", "LG",
                    "Màn hình 1440p 144Hz IPS, tuyệt đẹp. Chất lượng màu sắc chính xác.",
                    "Kích thước: 27\" | Độ phân giải: 2560x1440 (1440p) | Tần số: 144Hz | Panel: IPS | HDR: HDR400 | Response: 1ms GtG",
                    "monitor", "lg-ultragear-27-1440p-front.jpg", 15),

                ("ASUS ROG Swift 360Hz", 14990000m, 17990000m, "Màn hình", "ASUS",
                    "Màn hình 1440p 360Hz, đỉnh cao esports. NVIDIA G-Sync premium.",
                    "Kích thước: 27\" | Độ phân giải: 2560x1440 | Tần số: 360Hz | Panel: IPS | Response: 0.03ms GtG | G-Sync Premium",
                    "monitor", "asus-rog-swift-360hz-black-angle.jpg", 17),

                ("Dell S3422DWG Ultrawide", 12490000m, 14990000m, "Màn hình", "DELL",
                    "Màn hình ultrawide 34\", công thái học tuyệt vời. Curved panel thoải mái mắt.",
                    "Kích thước: 34\" | Độ phân giải: 3440x1440 | Tần số: 144Hz | Panel: IPS Curved | Response: 1ms GtG | HDMI/DP",
                    "monitor", "dell-s3422dwg-ultrawide-black-front.jpg", 17),

                ("LG 27UP550 4K", 9990000m, 11990000m, "Màn hình", "LG",
                    "Màn hình 4K 60Hz, màu sắc chính xác 98% DCI-P3. USB-C docking.",
                    "Kích thước: 27\" | Độ phân giải: 3840x2160 (4K) | Tần số: 60Hz | Panel: IPS | Color Accuracy: 98% DCI-P3 | USB-C DP alt mode",
                    "monitor", "lg-27up550-4k-white-front.jpg", 17),

                // ===== PHỤ KIỆN (6 sản phẩm) =====
                ("Lót Chuột SteelSeries QcK Prism", 890000m, 1190000m, "Loa, Micro, Webcam", "STEELSERIES",
                    "Lót chuột gaming XL 900x300mm, chất lượng cao. RGB Prism siêu đẹp.",
                    "Kích thước: 900x300x4mm | Chất liệu: Cloth | RGB: Prism 16M colors | Anti-slip: Rubber backing",
                    "accessories", "steelseries-qck-prism-mat-black-top.jpg", 25),

                ("HyperX Alloy Elite RGB", 2290000m, 2790000m, "Bàn phím", "HYPERX",
                    "Bàn phím cơ chất lượng tốt giá rẻ. Cherry MX Red êm ái.",
                    "Layout: Full Size | Switch: Cherry MX Red | Keycaps: Double Shot | RGB: Per-Key | Kết nối: USB 2.0 | Aluminum Frame",
                    "accessories", "hyperx-alloy-elite-rgb-angle.jpg", 18),

                ("Corsair MM1000 Qi", 1990000m, 2490000m, "Loa, Micro, Webcam", "CORSAIR",
                    "Đế sạc không dây kiêm lót chuột, tiện lợi. Sạc nhanh 10W.",
                    "Kích thước: 350x250mm | Chất liệu: Cloth | Sạc: Wireless Qi 10W | RGB: Corsair Link | Chống trượt: Rubber base",
                    "accessories", "corsair-mm1000-qi-charging-top.jpg", 20),

                ("NZXT Camflow", 1290000m, 1590000m, "Loa, Micro, Webcam", "NZXT",
                    "Webcam 1080p 60fps, tuyệt vời cho stream. Mic tích hợp tốt.",
                    "Độ phân giải: 1080p | Tần số: 60fps | Auto-focus | Mic tích hợp | Góc nhìn: 90° | Kết nối: USB 2.0",
                    "accessories", "nzxt-camflow-1080p-webcam-front.jpg", 19),

                ("Elgato Wave:3", 2190000m, 2590000m, "Loa, Micro, Webcam", "ELGATO",
                    "Microphone USB chất lượng cao cho streamer. Wave Cancellation tuyệt vời.",
                    "Loại: USB Condenser | Tần số: 20Hz-20kHz | Wave Cancellation | Tap-to-Mute | Mix Knob | Cảm biến LED",
                    "accessories", "elgato-wave-3-microphone-side.jpg", 15),

                ("AmazonBasics USB Hub 3.0 7-Port", 590000m, 790000m, "Loa, Micro, Webcam", "AMAZON",
                    "Hub USB 3.0 7 cổng, cấp điện riêng. Tốc độ 5Gbps.",
                    "Ports: 7 USB 3.0 | Tốc độ: 5Gbps | Nguồn: Power Adapter 12V/4A | LED chỉ báo | Dây cáp: 150cm",
                    "accessories", "amazon-basics-usb-hub-front.jpg", 25),

                // ===== LINH KIỆN (4 sản phẩm) =====
                ("Kingston Fury Beast 32GB DDR5", 3490000m, 4290000m, "Ổ cứng, RAM, Thẻ nhớ", "KINGSTON",
                    "RAM DDR5 32GB cho gaming/workstation hiệu năng cao. Tần số 6000MHz.",
                    "Dung lượng: 32GB | Tần số: 6000MHz | CAS Latency: 30 | Công nghệ: EXPO | Điện áp: 1.4V | Warranty: Lifetime",
                    "components", "kingston-fury-beast-ddr5-angle.jpg", 19),

                ("Samsung 990 Pro 2TB NVMe", 4990000m, 5990000m, "Ổ cứng, RAM, Thẻ nhớ", "SAMSUNG",
                    "SSD NVMe PCIe 4.0 siêu nhanh, đỉnh cao. Tốc độ 7100MB/s.",
                    "Dung lượng: 2TB | Giao tiếp: PCIe 4.0 NVMe | Tốc độ: Read 7100MB/s | Công nghệ: V-NAND | Warranty: 5 năm",
                    "components", "samsung-990-pro-angle.jpg", 17),

                ("Corsair RM1000e 1000W", 4290000m, 5290000m, "Case, Nguồn, Tản", "CORSAIR",
                    "Nguồn 1000W 80+ Gold, hiệu suất cao. Quạt 135mm êm ái.",
                    "Công suất: 1000W | Hiệu suất: 80+ Gold | Fan: 135mm FDB | Đầu nối: Fully Modular | Warranty: 10 năm | Âm thanh: < 20dB",
                    "components", "corsair-rm1000e-psu-angle.jpg", 19),

                ("Noctua NH-U12A CPU Cooler", 1790000m, 2190000m, "Case, Nguồn, Tản", "NOCTUA",
                    "Tản nhiệt không khí cao cấp, yên tĩnh. Tiếp xúc tối ưu.",
                    "Loại: Air Cooler 120mm | Tương thích: LGA1700/AM5 | TDP: Tối đa 250W | Fan: Noctua NF-A12x25 PWM | Âm thanh: < 20 dB | Bảo hành: 12 năm",
                    "components", "noctua-nh-u12a-cooler-angle.jpg", 18)
            };

            // Tạo brand map
            var brandMap = brands.ToDictionary(b => b.Name, b => b.Id);
            var categoryMap = categories.ToDictionary(c => c.Name, c => c.Id);

            // Seed sản phẩm
            foreach (var prod in products)
            {
                if (!brandMap.TryGetValue(prod.brand, out var brandId) || 
                    !categoryMap.TryGetValue(prod.categoryName, out var catId))
                    continue;

                var sp = new Product
                {
                    Name = prod.name,
                    Price = prod.price,
                    ListPrice = prod.msrp,
                    CategoryId = catId,
                    BrandId = brandId,
                    Status = 1,
                    CreatedAt = DateTime.Now.AddDays(-random.Next(1, 60)),
                    DiscountNote = prod.discount > 0 
                        ? $"Giảm {prod.discount}% hôm nay|Tặng Balo Gaming HDK|Vệ sinh máy miễn phí" 
                        : "Tặng Balo Gaming HDK|Giao hàng nhanh trong 24h",
                    WarrantyInfo = "24 Tháng chính hãng",
                    Description = $"<h5 class='text-danger fw-bold'>🎯 Đặc điểm nổi bật</h5><ul><li>✓ {prod.description}</li><li>✓ Bảo hành chính hãng 24 tháng từ HDKTech</li><li>✓ Giao hàng toàn quốc trong 24-48 giờ</li><li>✓ Hỗ trợ trả góp 0% lãi suất</li></ul>",
                    Specifications = prod.specs
                };

                context.Products.Add(sp);
            }

            await context.SaveChangesAsync();

            // Seed ảnh cho từng sản phẩm
            var spList = await context.Products.ToListAsync();
            foreach (var sp in spList.Take(44)) // 44 sản phẩm
            {
                var prodData = products[spList.IndexOf(sp)];
                context.ProductImages.Add(new ProductImage
                {
                    ProductId = sp.Id,
                    ImageUrl = $"{prodData.imageFolder}/{prodData.imageFile}",
                    IsDefault = true,
                    CreatedAt = DateTime.Now
                });

                // Thêm kho hàng
                context.Inventories.Add(new Inventory
                {
                    ProductId = sp.Id,
                    Quantity = random.Next(10, 100),
                    UpdatedAt = DateTime.Now
                });
            }

            await context.SaveChangesAsync();
        }
    }
}


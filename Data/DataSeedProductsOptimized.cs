using Bogus;
using HDKTech.ChucNangPhanQuyen;
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HDKTech.Areas.Identity.Data
{
    public static class DataSeedProductsOptimized
    {
        /// <summary>
        /// STRUCTURED TECHNICAL SPECS FORMAT:
        /// "Key1: Value1 | Key2: Value2 | Key3: Value3"
        /// Example: "CPU: Intel Core i7-13650HX | RAM: 16GB | SSD: 512GB | VGA: RTX 4050"
        /// </summary>
        public static async Task SeedProducts(IServiceProvider services)
        {
            var context = services.GetRequiredService<HDKTechContext>();

            // Skip if already seeded
            if (await context.Products.AnyAsync()) return;

            var brands = await context.Brands.ToListAsync();
            var categories = await context.Categories.Where(c => c.ParentCategoryId == null).ToListAsync();

            // Product data: (Name, Price, MSRP, CategoryName, Brand, Description, StructuredSpecs, Image, Discount)
            var products = new List<(string, decimal, decimal?, string, string, string, string, string, int)>
            {
                // ========== LAPTOP (8 sản phẩm) ==========
                (
                    "Dell XPS 13 Plus Gen 14 - Thiết kế Infinity",
                    24990000m, 27990000m, "Laptop", "DELL",
                    "Ultrabook siêu mỏng, hiệu năng mạnh cho công việc. Thiết kế INFINITY Edge, bàn phím ẩn hiện",
                    "CPU: Intel Core i5-1340P | RAM: 8GB LPDDR5 | SSD: 512GB NVMe | Display: 13.4\" FHD+ | GPU: Intel Iris Xe | Pin: 55Wh",
                    "laptops/dell-xps-13-plus.png", 11
                ),
                (
                    "ASUS VivoBook 14 - Ryzen 5 5500U",
                    11990000m, 13990000m, "Laptop", "ASUS",
                    "Laptop lightweight với AMD Ryzen 5, thích hợp du lịch và học tập. Nặng chỉ 1.4kg",
                    "CPU: AMD Ryzen 5 5500U | RAM: 8GB DDR4 | SSD: 512GB NVMe | Display: 14\" FHD | GPU: Radeon Graphics | Pin: 42Wh",
                    "laptops/asus-vivobook-14.png", 14
                ),
                (
                    "Lenovo ThinkPad X1 Carbon Gen 12 - Premium Business",
                    32990000m, 36990000m, "Laptop", "LENOVO",
                    "Dòng máy cao cấp cho doanh nhân. Bàn phím ThinkPad legendary, thoại mái cho người dùng lâu",
                    "CPU: Intel Core i7-1365U | RAM: 16GB LPDDR5 | SSD: 512GB NVMe | Display: 14\" OLED | GPU: Intel Iris Xe | Pin: 52Wh",
                    "laptops/lenovo-thinkpad-x1.png", 11
                ),
                (
                    "Apple MacBook Air M3 - 2024",
                    28990000m, null, "Laptop", "APPLE",
                    "Chip Apple M3 mạnh mẽ, thời lượng pin 15+ giờ. Màn hình Liquid Retina tuyệt đẹp",
                    "CPU: Apple M3 | RAM: 8GB unified | SSD: 256GB NVMe | Display: 13.6\" Liquid Retina | GPU: 8-core GPU | Pin: 52.6Wh",
                    "laptops/macbook-air-m3.png", 0
                ),
                (
                    "HP Pavilion 15 - Intel Core i5 Gen 13",
                    12990000m, 15290000m, "Laptop", "HP",
                    "Laptop giá rẻ, hiệu năng ổn định cho học tập và công việc văn phòng",
                    "CPU: Intel Core i5-1335U | RAM: 8GB DDR4 | SSD: 256GB NVMe | Display: 15.6\" FHD | GPU: Intel Iris Xe | Pin: 41Wh",
                    "laptops/hp-pavilion-15.png", 15
                ),
                (
                    "Acer Aspire 5 - Ryzen 7 5700U",
                    14990000m, 17990000m, "Laptop", "ACER",
                    "Màn hình 15.6\" IPS với hiệu năng tốt. Tản nhiệt kép, hệ thống làm mát tốt",
                    "CPU: AMD Ryzen 7 5700U | RAM: 16GB DDR4 | SSD: 512GB NVMe | Display: 15.6\" IPS | GPU: Radeon Graphics | Pin: 48Wh",
                    "laptops/acer-aspire-5.png", 17
                ),
                (
                    "ASUS VivoBook Pro 16 - Ryzen 9",
                    38990000m, 44990000m, "Laptop", "ASUS",
                    "Laptop hiệu suất cao cho creator. Màn hình 16\" OLED, bàn phím chiclet tuyệt đẹp",
                    "CPU: AMD Ryzen 9 5900HX | RAM: 32GB DDR4 | SSD: 1TB NVMe | Display: 16\" OLED | GPU: Radeon RX 6700M | Pin: 90Wh",
                    "laptops/asus-vivobook-pro-16.png", 13
                ),
                (
                    "MSI Modern 15 - Intel Core i7 EVO",
                    19990000m, 23990000m, "Laptop", "MSI",
                    "Laptop mỏng nhẹ cho doanh nhân hiện đại. Cấu hình mạnh, giá cạnh tranh",
                    "CPU: Intel Core i7-1195G7 EVO | RAM: 16GB DDR4 | SSD: 512GB NVMe | Display: 15.6\" FHD | GPU: Intel Iris Xe | Pin: 52.5Wh",
                    "laptops/msi-modern-15.png", 13
                ),

                // ========== LAPTOP GAMING (8 sản phẩm) ==========
                (
                    "ASUS ROG Strix G16 - RTX 4090 Monster",
                    49990000m, 59990000m, "Laptop Gaming", "ASUS",
                    "Laptop gaming flagship, GPU RTX 4090 quái vật. Màn hình 16\" 240Hz, hiệu năng đỉnh cao",
                    "CPU: Intel Core i9-13950HX | RAM: 32GB DDR5 | SSD: 1TB NVMe | Display: 16\" 240Hz | GPU: RTX 4090 | Pin: 90Wh",
                    "laptops-gaming/asus-rog-strix-g16.png", 17
                ),
                (
                    "MSI Katana 17 GF - RTX 4070",
                    27990000m, 32990000m, "Laptop Gaming", "MSI",
                    "Gaming laptop 17\", RTX 4070, màn hình 144Hz IPS. Thiết kế gọn nhẹ cho player",
                    "CPU: Intel Core i7-12700H | RAM: 16GB DDR4 | SSD: 512GB NVMe | Display: 17\" 144Hz IPS | GPU: RTX 4070 | Pin: 99Wh",
                    "laptops-gaming/msi-katana-17.png", 15
                ),
                (
                    "Acer Predator 18 - RTX 4080 Beast",
                    42990000m, 49990000m, "Laptop Gaming", "ACER",
                    "Màn hình 18\" khổng lồ, RTX 4080 siêu mạnh. Hệ thống tản nhiệt 7 ống đồng",
                    "CPU: Intel Core i9-13900HX | RAM: 32GB DDR5 | SSD: 1TB NVMe | Display: 18\" 120Hz | GPU: RTX 4080 | Pin: 99Wh",
                    "laptops-gaming/acer-predator-18.png", 14
                ),
                (
                    "Lenovo Legion 9 Pro - RTX 4090 Laptop",
                    59990000m, 69990000m, "Laptop Gaming", "LENOVO",
                    "Dòng cao cấp nhất của Legion, performance đỉnh cao. Màn hình OLED 16\" 240Hz",
                    "CPU: Intel Core i9-13900HX | RAM: 32GB DDR5 | SSD: 1TB NVMe | Display: 16\" OLED 240Hz | GPU: RTX 4090 | Pin: 99Wh",
                    "laptops-gaming/lenovo-legion-9-pro.png", 14
                ),
                (
                    "GIGABYTE Aorus 15 - RTX 4060 Entry",
                    18990000m, 22990000m, "Laptop Gaming", "GIGABYTE",
                    "Gaming laptop entry-level, RTX 4060. Giá tốt cho bạn chơi game casual",
                    "CPU: Intel Core i5-12450H | RAM: 16GB DDR4 | SSD: 512GB NVMe | Display: 15.6\" 144Hz | GPU: RTX 4060 | Pin: 60Wh",
                    "laptops-gaming/gigabyte-aorus-15.png", 17
                ),
                (
                    "MSI Raider GE78 HX - RTX 4070 Ti Pro",
                    39990000m, 45990000m, "Laptop Gaming", "MSI",
                    "Màn hình 240Hz, chip tân trang nhất. Bộ tản nhiệt Dynamic Boost",
                    "CPU: Intel Core i7-13700HX | RAM: 32GB DDR4 | SSD: 1TB NVMe | Display: 17.3\" 240Hz | GPU: RTX 4070 Ti | Pin: 99Wh",
                    "laptops-gaming/msi-raider-ge78.png", 13
                ),
                (
                    "ASUS TUF Gaming A17 - RTX 4050",
                    15990000m, 18990000m, "Laptop Gaming", "ASUS",
                    "Gaming laptop cứng cáp TUF series. Hiệu năng gaming mượt cho game 1080p",
                    "CPU: AMD Ryzen 7 5800H | RAM: 16GB DDR4 | SSD: 512GB NVMe | Display: 17.3\" 144Hz | GPU: RTX 4050 | Pin: 90Wh",
                    "laptops-gaming/asus-tuf-a17.png", 16
                ),
                (
                    "Alienware m17 R5 - RTX 3080 Ti Professional",
                    55990000m, 64990000m, "Laptop Gaming", "DELL",
                    "Gaming laptop chuyên nghiệp, hiệu năng cực đỉnh. Màn hình OLED 1000Hz",
                    "CPU: Intel Core i9-12900HK | RAM: 32GB DDR5 | SSD: 2TB NVMe | Display: 17\" OLED 1000Hz | GPU: RTX 3080 Ti | Pin: 140Wh",
                    "laptops-gaming/alienware-m17.png", 14
                ),

                // ========== PC GAMING (8 sản phẩm) ==========
                (
                    "PC Gaming ASUS ROG i7-12700K RTX 3070 Ti",
                    29990000m, 35990000m, "PC GVN", "ASUS",
                    "PC gaming cấu hình cao, chơi game AAA siêu mượt. Case RGB đẹp mắt",
                    "CPU: Intel Core i7-12700K | RAM: 32GB DDR4 3600MHz | SSD: 1TB NVMe Gen4 | GPU: RTX 3070 Ti | PSU: 850W 80+ Gold",
                    "components/pc-asus-rog-rtx3070ti.png", 17
                ),
                (
                    "MSI Aegis RS - RTX 4090 Ultra",
                    52990000m, 62990000m, "PC GVN", "MSI",
                    "PC gaming siêu cao cấp, cấu hình bá đạo. Hệ thống làm mát AIO 360mm",
                    "CPU: Intel Core i9-13900K | RAM: 64GB DDR5 | SSD: 2TB NVMe Gen5 | GPU: RTX 4090 | PSU: 1200W 80+ Platinum",
                    "components/msi-aegis-rs-rtx4090.png", 16
                ),
                (
                    "PC Gaming NZXT BLD - RTX 2060 Basic",
                    12990000m, 15990000m, "PC GVN", "NZXT",
                    "PC gaming cơ bản, phù hợp gaming casual. Cấu hình entry-level giá tốt",
                    "CPU: AMD Ryzen 5 5600X | RAM: 16GB DDR4 3200MHz | SSD: 500GB NVMe | GPU: RTX 2060 | PSU: 650W 80+ Bronze",
                    "components/pc-nzxt-bld-rtx2060.png", 19
                ),
                (
                    "Corsair Graphite - RTX 3080 Pro Gaming",
                    35990000m, 42990000m, "PC GVN", "CORSAIR",
                    "Case cao cấp Corsair, cấu hình chuyên game. Hệ thống nước AIO 280mm",
                    "CPU: Intel Core i7-12700 | RAM: 32GB DDR4 3200MHz | SSD: 1TB NVMe Gen4 | GPU: RTX 3080 | PSU: 1000W 80+ Gold",
                    "components/pc-corsair-graphite-rtx3080.png", 16
                ),
                (
                    "Lian Li Lancool - RTX 4070 Pro",
                    31990000m, 38990000m, "PC GVN", "LIAN LI",
                    "Case Lancool siêu đẹp, hiệu năng ổn định. Quạt RGB 3x120mm tặng",
                    "CPU: Intel Core i5-12600K | RAM: 32GB DDR4 3200MHz | SSD: 1TB NVMe Gen4 | GPU: RTX 4070 | PSU: 800W 80+ Gold",
                    "components/pc-lian-li-lancool-rtx4070.png", 18
                ),
                (
                    "GIGABYTE AORUS - RTX 3090 Ti Monster",
                    54990000m, 64990000m, "PC GVN", "GIGABYTE",
                    "PC quái vật, RTX 3090 Ti cực khủng. Mainboard Z790 AORUS Master cao cấp",
                    "CPU: Intel Core i9-12900K | RAM: 64GB DDR4 3600MHz | SSD: 2TB NVMe Gen4 | GPU: RTX 3090 Ti | PSU: 1200W 80+ Platinum",
                    "components/pc-gigabyte-aorus-rtx3090ti.png", 15
                ),
                (
                    "MSI MPG - Ryzen 9 7950X RTX 4080",
                    47990000m, 55990000m, "PC GVN", "MSI",
                    "PC hiệu năng cực cao, Ryzen 9 + RTX 4080. Mainboard MPG Z790 Edge WiFi",
                    "CPU: AMD Ryzen 9 7950X | RAM: 32GB DDR5 6000MHz | SSD: 2TB NVMe Gen5 | GPU: RTX 4080 | PSU: 1000W 80+ Platinum",
                    "components/pc-msi-mpg-ryzen9-rtx4080.png", 14
                ),
                (
                    "Custom Build - i9-12900K RTX 4070 Mid-High",
                    38990000m, 44990000m, "PC GVN", "ASUS",
                    "PC custom build cân bằng performance-price. Hệ thống AIO 240mm",
                    "CPU: Intel Core i9-12900K | RAM: 32GB DDR4 3600MHz | SSD: 1TB NVMe Gen4 | GPU: RTX 4070 | PSU: 850W 80+ Gold",
                    "components/pc-custom-build-mid-high.png", 13
                ),

                // ========== COMPONENTS (8 sản phẩm) ==========
                (
                    "ASUS ROG STRIX Z890-E - Socket LGA1700",
                    7990000m, 9490000m, "Main, CPU, VGA", "ASUS",
                    "Bo mạch chủ gaming cao cấp, hỗ trợ DDR5 và PCIe 5.0",
                    "Socket: LGA1700 | Chipset: Z890 | RAM: DDR5 | PCIe: 5.0 | WiFi: WiFi 7 | Audio: SupremeFX S1220",
                    "components/motherboard-asus-z890.png", 16
                ),
                (
                    "Intel Core i9-14900KS - 24 Cores Beast",
                    16990000m, 19990000m, "Main, CPU, VGA", "INTEL",
                    "CPU gaming flagship, 24 cores/32 threads, 6.2GHz Turbo",
                    "Cores: 24C/32T | Frequency: 6.2GHz Max | Cache: 36MB L3 | Socket: LGA1700 | TDP: 150W | Arch: Raptor Lake",
                    "components/cpu-intel-i9-14900ks.png", 15
                ),
                (
                    "NVIDIA RTX 5090 - 32GB GDDR7 Beast",
                    89990000m, 99990000m, "Main, CPU, VGA", "NVIDIA",
                    "GPU gaming quái vật, VRAM 32GB GDDR7, 21760 CUDA cores",
                    "Memory: 32GB GDDR7 | CUDA: 21760 | Interface: PCIe 5.0 | TDP: 575W | Boost: 2.5GHz | Arch: Ada Generation",
                    "components/gpu-nvidia-rtx5090.png", 10
                ),
                (
                    "Samsung 870 QVO - 4TB SSD SATA",
                    3990000m, 4790000m, "Main, CPU, VGA", "SAMSUNG",
                    "SSD SATA tốc độ đọc ghi ổn định, dung lượng lớn 4TB",
                    "Capacity: 4TB | Interface: SATA III | Speed: 560MB/s Read | Endurance: 4,800TBW | Form: 2.5 inch",
                    "storage/ssd-samsung-870qvo-4tb.png", 17
                ),
                (
                    "Corsair MP600 ELITE XT - 2TB NVMe Gen4",
                    2990000m, 3590000m, "Main, CPU, VGA", "CORSAIR",
                    "SSD NVMe Gen4, tốc độ đọc 4950MB/s, hiệu suất cao",
                    "Capacity: 2TB | Interface: PCIe 4.0 NVMe | Speed: 4950MB/s | Endurance: 1,800TBW | DRAM: Yes",
                    "storage/ssd-corsair-mp600-elite-2tb.png", 14
                ),
                (
                    "Kingston Fury Beast - 32GB DDR5 6000MHz",
                    5490000m, 6490000m, "Main, CPU, VGA", "KINGSTON",
                    "RAM DDR5 tốc độ cao, 32GB dung lượng, hoạt động ổn định",
                    "Capacity: 32GB (16GBx2) | Speed: 6000MHz | Type: DDR5 | Voltage: 1.4V | CAS: CL30 | RGB: Yes",
                    "components/ram-kingston-fury-ddr5-32gb.png", 15
                ),
                (
                    "Noctua NH-D15 - CPU Cooler Khí",
                    3490000m, 4190000m, "Main, CPU, VGA", "NOCTUA",
                    "Tản CPU khí đơn kip hiệu suất cao, im lặng, có thể sắp xếp với RAM cao",
                    "Type: Air | TDP: 250W max | Fans: 2x140mm | Socket: LGA1700/1200/1150 | Height: 164mm | Color: Brown+Tan",
                    "components/cooler-noctua-nh-d15.png", 13
                ),
                (
                    "Corsair AX1600i - 1600W PSU 80+ Titanium",
                    8990000m, 10490000m, "Main, CPU, VGA", "CORSAIR",
                    "Nguồn Corsair công suất 1600W, hiệu suất Titanium 80+",
                    "Wattage: 1600W | Efficiency: 80+ Titanium | Modular: Full | Fans: 3x140mm | Cables: All Modular | Warranty: 12y",
                    "components/psu-corsair-ax1600i.png", 14
                ),

                // ========== PERIPHERALS - CHUỘT (8 sản phẩm) ==========
                (
                    "Logitech G Pro X Superlight 2 - Gaming Mouse",
                    2490000m, 2990000m, "Chuột + Lót chuột", "LOGITECH",
                    "Chuột gaming siêu nhẹ 2.2g, 1ms latency, đẳng cấp esports",
                    "Weight: 2.2g | DPI: 25600 | Wireless: 2.4GHz | Battery: 70h | Sensor: HERO 25K | Buttons: 5",
                    "peripherals/mouse-logitech-pro-x-sl2.png", 17
                ),
                (
                    "Razer Viper V3 Pro - Precision Gaming",
                    2990000m, 3490000m, "Chuột + Lót chuột", "RAZER",
                    "Chuột siêu nhạy, sensor PixelFocus HD, khí động học tối ưu",
                    "Weight: 2.54g | DPI: 30000 | Wireless: 2.4GHz | Battery: 100h | Sensor: PixelFocus HD | Buttons: 8",
                    "peripherals/mouse-razer-viper-v3-pro.png", 14
                ),
                (
                    "SteelSeries Rival 5 - Ergonomic Gaming",
                    1990000m, 2490000m, "Chuột + Lót chuột", "STEELSERIES",
                    "Chuột công thái học, phù hợp dùng lâu, tùy chỉnh trọng lượng",
                    "Weight: 98g adjustable | DPI: 18000 | Wired: USB | Sensor: TrueMove Pro | Buttons: 9 | Color: Black",
                    "peripherals/mouse-steelseries-rival5.png", 20
                ),
                (
                    "Corsair Sabre RGB Pro - Balanced Gaming",
                    1490000m, 1890000m, "Chuột + Lót chuột", "CORSAIR",
                    "Chuột chất lượng tốt giá hợp lý, RGB Corsair Link",
                    "Weight: 99g | DPI: 18000 | Wired: USB | Sensor: PMW3389 | Buttons: 7 | RGB: Per-Button",
                    "peripherals/mouse-corsair-sabre-rgb.png", 21
                ),
                (
                    "ASUS ROG Gladius III Pro - Wireless Gaming",
                    2690000m, 3190000m, "Chuột + Lót chuột", "ASUS",
                    "Chuột gaming wireless ASUS, đáp ứng esports",
                    "Weight: 2.5g | DPI: 32000 | Wireless: 2.4GHz | Battery: 100h | Sensor: PMW3389 | Buttons: 7",
                    "peripherals/mouse-asus-gladius-iii-pro.png", 16
                ),
                (
                    "Logitech MX Master 3S - Productivity Master",
                    3990000m, 4690000m, "Chuột + Lót chuột", "LOGITECH",
                    "Chuột cao cấp cho productivity, smooth scrolling, multi-device",
                    "Weight: 141g | DPI: 8000 | Wireless: 2.4GHz+BT | Battery: 70d | Sensor: HERO 4K | Buttons: 8",
                    "peripherals/mouse-logitech-mx-master3s.png", 15
                ),
                (
                    "HyperX Pulsefire Haste 2 - Lightweight Gaming",
                    1290000m, 1590000m, "Chuột + Lót chuột", "HYPERX",
                    "Chuột gaming nhẹ HyperX, phù hợp esports, giá rẻ",
                    "Weight: 59g | DPI: 26000 | Wired: USB | Sensor: PMW3389 | Buttons: 6 | Design: Honeycomb Shell",
                    "peripherals/mouse-hyperx-pulsefire-haste2.png", 19
                ),
                (
                    "SteelSeries Aerox 5 Wireless - Pro Gaming",
                    2190000m, 2590000m, "Chuột + Lót chuột", "STEELSERIES",
                    "Chuột gaming wireless, honeycomb shell, đẹp và mạnh",
                    "Weight: 65g | DPI: 26000 | Wireless: 2.4GHz | Battery: 200h | Sensor: TrueMove Pro | Buttons: 9",
                    "peripherals/mouse-steelseries-aerox5-wireless.png", 15
                ),

                // ========== PERIPHERALS - BÀN PHÍM (8 sản phẩm) ==========
                (
                    "AKKO MOD007 Bi-Color - Hot-Swap Mechanical",
                    2990000m, 3490000m, "Bàn phím", "AKKO",
                    "Bàn phím cơ hot-swap, tùy chỉnh full, bộ switch đổi được",
                    "Layout: 75% | Switch: Hot-Swap | Switches: Akko V3 Pro | RGB: Per-Key | Stabilizers: Screw-in | Price: 2.99M",
                    "peripherals/keyboard-akko-mod007.png", 14
                ),
                (
                    "Corsair K100 RGB Platinum - Premium Mechanical",
                    5490000m, 6490000m, "Bàn phím", "CORSAIR",
                    "Bàn phím cao cấp, tổng hợp khâu, aluminum frame",
                    "Layout: Full-Size | Switch: Corsair OPX | RGB: Per-Key | Wired: USB | Material: Aluminum | Extra: Macro Keys",
                    "peripherals/keyboard-corsair-k100-platinum.png", 15
                ),
                (
                    "Durgod Hades 68 - Compact Budget Gaming",
                    1890000m, 2290000m, "Bàn phím", "DURGOD",
                    "Bàn phím gaming giá tốt, 68 phím compact",
                    "Layout: 68-Key | Switch: Outemu Blue | RGB: Single Color | Wired: USB Type-C | Material: ABS | Price: 1.89M",
                    "peripherals/keyboard-durgod-hades68.png", 18
                ),
                (
                    "Keychron Q1 Pro - Wireless+Wired Mechanical",
                    2890000m, 3390000m, "Bàn phím", "KEYCHRON",
                    "Bàn phím 75%, wireless + wired, pin 168 giờ",
                    "Layout: 75% | Switch: Gateron G Pro | RGB: Per-Key | Wireless: Bluetooth 5.1 | Battery: 168h | Material: Aluminum",
                    "peripherals/keyboard-keychron-q1-pro.png", 15
                ),
                (
                    "Varmilo VA88M - Wireless Mechanical",
                    4290000m, 4990000m, "Bàn phím", "VARMILO",
                    "Bàn phím cơ cao cấp, vỏ gỗ, công thái học tuyệt đẹp",
                    "Layout: 88-Key Variant | Switch: Varmilo EC V2 | RGB: Custom LED | Wireless: Bluetooth | Material: Wood",
                    "peripherals/keyboard-varmilo-va88m.png", 14
                ),
                (
                    "SteelSeries Apex 9 - Mechanical Gaming",
                    3490000m, 4090000m, "Bàn phím", "STEELSERIES",
                    "Bàn phím gaming SteelSeries, OLED screen, mecha-membrane switches",
                    "Layout: Full-Size | Switch: SteelSeries Mecha-Membrane | RGB: Per-Key | Wired: USB | OLED: 2.0 inch",
                    "peripherals/keyboard-steelseries-apex9.png", 15
                ),
                (
                    "Razer Huntsman V3 Pro - Optical Gaming",
                    2990000m, 3490000m, "Bàn phím", "RAZER",
                    "Bàn phím gaming optical, tốc độ đáp ứng cực nhanh",
                    "Layout: Full-Size | Switch: Razer Optical | RGB: Per-Key | Wired: USB | Typing: Linear/Clicky",
                    "peripherals/keyboard-razer-huntsman-v3.png", 14
                ),
                (
                    "Drop Alt High Profile - Custom Mechanical",
                    2290000m, 2690000m, "Bàn phím", "DROP",
                    "Bàn phím custom enthusiast, hỗ trợ hot-swap",
                    "Layout: 65% | Switch: Hot-Swap | RGB: Per-Key | Material: Aluminum | Programmable: QMK Compatible | Price: 2.29M",
                    "peripherals/keyboard-drop-alt-hp.png", 15
                ),

                // ========== PERIPHERALS - TAI NGHE (8 sản phẩm) ==========
                (
                    "HyperX Cloud Stinger 2 - Budget Gaming",
                    1490000m, 1890000m, "Tai nghe", "HYPERX",
                    "Tai nghe gaming giá rẻ mà tốt, sound cân bằng",
                    "Type: Over-Ear | Driver: 50mm | Impedance: 32Ω | Frequency: 20Hz-20kHz | Weight: 190g | Cable: Detachable 3.5mm",
                    "audio/headset-hyperx-cloud-stinger2.png", 21
                ),
                (
                    "Corsair HS80 RGB - Wireless Gaming Premium",
                    2490000m, 2990000m, "Tai nghe", "CORSAIR",
                    "Tai nghe gaming cao cấp, wireless 2.4GHz, surround sound",
                    "Type: Over-Ear | Driver: 50mm | Wireless: 2.4GHz | Battery: 20h | Frequency: 20Hz-20kHz | Weight: 295g",
                    "audio/headset-corsair-hs80-rgb.png", 17
                ),
                (
                    "Razer Kraken V3 - Immersive 3D Audio",
                    2290000m, 2690000m, "Tai nghe", "RAZER",
                    "Tai nghe tạo âm thanh 360, immersive gaming",
                    "Type: Over-Ear | Driver: 50mm | Frequency: 20Hz-20kHz | Weight: 320g | Cable: Wired USB | Mic: Noise-Canceling",
                    "audio/headset-razer-kraken-v3.png", 15
                ),
                (
                    "SteelSeries Arctis 9 - Wireless Premium",
                    3490000m, 3990000m, "Tai nghe", "STEELSERIES",
                    "Tai nghe wireless siêu nhạy, đèn LED rgb đẹp",
                    "Type: Over-Ear | Driver: 40mm | Wireless: 2.4GHz | Battery: 20h | Frequency: 20Hz-20kHz | Weight: 302g",
                    "audio/headset-steelseries-arctis9.png", 13
                ),
                (
                    "ASUS ROG Delta S - Gaming Audio Beast",
                    2690000m, 3190000m, "Tai nghe", "ASUS",
                    "Tai nghe gaming ASUS ROG, công thái học, âm thanh định hướng chính xác",
                    "Type: Over-Ear | Driver: 50mm | Frequency: 20Hz-40kHz | Weight: 320g | Cable: USB Type-C | Mic: Noise-Canceling",
                    "audio/headset-asus-rog-delta-s.png", 15
                ),
                (
                    "JBL Quantum 910 - Wireless eSports",
                    3290000m, 3890000m, "Tai nghe", "JBL",
                    "Tai nghe gaming JBL, headband thoải mái, battery dài",
                    "Type: Over-Ear | Driver: 40mm | Wireless: 2.4GHz | Battery: 32h | Frequency: 20Hz-20kHz | Weight: 340g",
                    "audio/headset-jbl-quantum910.png", 15
                ),
                (
                    "Sony WH-1000XM5 - Noise Cancelling Master",
                    5490000m, 6490000m, "Tai nghe", "SONY",
                    "Tai nghe cao cấp, khử tiếng ồn chủ động tốt nhất",
                    "Type: Over-Ear | Driver: 40mm | Wireless: Bluetooth 5.3 | Battery: 12h | ANC: Yes | Weight: 250g",
                    "audio/headset-sony-wh1000xm5.png", 15
                ),
                (
                    "Audio Technica ATH-M50X - Studio Professional",
                    2990000m, 3490000m, "Tai nghe", "AUDIO TECHNICA",
                    "Tai nghe studio chuyên nghiệp, âm thanh trung thực",
                    "Type: Over-Ear | Driver: 45mm | Impedance: 38Ω | Frequency: 15Hz-28kHz | Weight: 190g | Cable: Coiled+Straight",
                    "audio/headset-audio-technica-m50x.png", 14
                ),

                // ========== MONITOR (8 sản phẩm) ==========
                (
                    "LG UltraGear 27\" 1440p 144Hz IPS",
                    8490000m, 9990000m, "Màn hình", "LG",
                    "Màn hình 1440p 144Hz IPS, tuyệt đẹp cho gaming",
                    "Panel: IPS 27\" | Resolution: 2560x1440 | Refresh: 144Hz | Response: 1ms | HDR: HDR400 | Features: G-Sync Compatible",
                    "monitor/monitor-lg-27gp850.png", 15
                ),
                (
                    "ASUS ROG Swift 360Hz - Esports Beast",
                    14990000m, 17990000m, "Màn hình", "ASUS",
                    "Màn hình 1440p 360Hz, đỉnh cao esports",
                    "Panel: IPS 27\" | Resolution: 2560x1440 | Refresh: 360Hz | Response: 0.03ms | G-Sync: Yes | Features: ROG Arena",
                    "monitor/monitor-asus-rog-swift-360hz.png", 17
                ),
                (
                    "Dell S3422DWG - Ultrawide 34\" Curved",
                    12490000m, 14990000m, "Màn hình", "DELL",
                    "Màn hình ultrawide 34\", công thái học tuyệt vời",
                    "Panel: VA 34\" Ultrawide | Resolution: 3440x1440 | Refresh: 144Hz | Response: 1ms | Curvature: 1800R | USB-C: Yes",
                    "monitor/monitor-dell-s3422dwg.png", 17
                ),
                (
                    "LG 27UP550 - 4K 60Hz Precision",
                    9990000m, 11990000m, "Màn hình", "LG",
                    "Màn hình 4K 60Hz, màu sắc chính xác 98% DCI-P3",
                    "Panel: IPS 27\" | Resolution: 3840x2160 | Refresh: 60Hz | Response: 5ms | Color: 98% DCI-P3 | USB-C: Yes",
                    "monitor/monitor-lg-27up550.png", 17
                ),
                (
                    "BenQ EW2780U - 4K Professional",
                    7990000m, 9490000m, "Màn hình", "BENQ",
                    "Màn hình 4K 60Hz, khoa học màu sắc, tư vấn chuyên nghiệp",
                    "Panel: IPS 27\" | Resolution: 3840x2160 | Refresh: 60Hz | Response: 5ms | Color: 99% Adobe RGB | USB-C: Yes",
                    "monitor/monitor-benq-ew2780u.png", 16
                ),
                (
                    "Acer Predator X27 - Gaming 1440p 240Hz",
                    11990000m, 13990000m, "Màn hình", "ACER",
                    "Màn hình gaming 1440p 240Hz, quantum dots, giá cân bằng",
                    "Panel: IPS 27\" | Resolution: 2560x1440 | Refresh: 240Hz | Response: 0.5ms | Technology: Quantum Dot | G-Sync: Yes",
                    "monitor/monitor-acer-predator-x27.png", 14
                ),
                (
                    "MSI MAG 321CURV QHD - Curved Gaming",
                    10490000m, 12290000m, "Màn hình", "MSI",
                    "Màn hình gaming curved 1440p 165Hz, responsive",
                    "Panel: VA 32\" Curved | Resolution: 2560x1440 | Refresh: 165Hz | Response: 1ms | Curvature: 1800R | FreeSync: Yes",
                    "monitor/monitor-msi-mag-321curv.png", 15
                ),
                (
                    "ASUS PA279CV - Color Accurate 1440p",
                    8490000m, 9990000m, "Màn hình", "ASUS",
                    "Màn hình chuyên nghiệp, 100% sRGB, phù hợp design",
                    "Panel: IPS 27\" | Resolution: 2560x1440 | Refresh: 60Hz | Response: 5ms | Color: 100% sRGB | USB-C: Yes",
                    "monitor/monitor-asus-pa279cv.png", 15
                )
            };

            // Add products to database
            int imageIndex = 1;
            foreach (var (name, price, msrp, categoryName, brandName, description, specs, imageName, discount) in products)
            {
                var category = categories.FirstOrDefault(c => c.Name == categoryName);
                var brand = brands.FirstOrDefault(b => b.Name == brandName);

                if (category == null || brand == null) continue;

                var product = new Product
                {
                    Name = name,
                    Price = price,
                    ListPrice = msrp,
                    Description = description,
                    Specifications = specs,  // Structured format
                    CategoryId = category.Id,
                    BrandId = brand.Id,
                    Status = 1,
                    WarrantyInfo = "24 Tháng chính hãng",
                    DiscountNote = "Miễn phí vận chuyển | Trả góp 0%",
                    CreatedAt = DateTime.Now.AddDays(-new Random().Next(1, 30))
                };

                context.Products.Add(product);
                await context.SaveChangesAsync();

                // Add image
                var image = new ProductImage
                {
                    Id = product.Id,
                    ImageUrl = imageName,
                    IsDefault = true,
                    CreatedAt = DateTime.Now
                };
                context.ProductImages.Add(image);

                imageIndex++;
            }

            await context.SaveChangesAsync();
        }
    }
}



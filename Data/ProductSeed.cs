using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    public static class ProductSeed
    {
        public static async Task SeedAsync(HDKTechContext context)
        {
            if (await context.Products.AnyAsync()) return;

            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Products] ON");

                    var now = DateTime.Now;
                    var rng = new Random(42);

            // Format specs: "Key: Value | Key: Value"
            var products = new List<(int id, string name, decimal price, decimal? listPrice,
                int catId, int brandId, string specs, string imgFolder, string imgFile, int status)>
            {
                // ── LAPTOP (1-10) ────────────────────────────────────────
                (1, "Dell XPS 13 Plus - Core i7 Gen 13", 26_990_000, 29_990_000,
                    SeedConstants.CatLaptop, SeedConstants.BrandDell,
                    "CPU: Intel Core i7-1360P | RAM: 16GB LPDDR5 | SSD: 512GB NVMe | GPU: Intel Iris Xe | Màn hình: 13.4\" FHD+ OLED | Pin: 55Wh",
                    "laptops", "dell-xps-13-silver-front.jpg", 1),

                (2, "ASUS VivoBook 15X - Ryzen 7 5700U", 14_990_000, 17_500_000,
                    SeedConstants.CatLaptop, SeedConstants.BrandAsus,
                    "CPU: AMD Ryzen 7 5700U | RAM: 16GB DDR4 | SSD: 512GB PCIe | GPU: Radeon Graphics | Màn hình: 15.6\" FHD IPS | Trọng lượng: 1.8kg",
                    "laptops", "asus-vivobook-14x-silver.jpg", 1),

                (3, "Lenovo ThinkPad X1 Carbon Gen 12", 34_990_000, 38_000_000,
                    SeedConstants.CatLaptop, SeedConstants.BrandLenovo,
                    "CPU: Intel Core Ultra 7 155H | RAM: 16GB LPDDR5X | SSD: 512GB Gen4 NVMe | GPU: Intel Arc | Màn hình: 14\" 2.8K OLED | Pin: 57Wh",
                    "laptops", "lenovo-thinkpad-x1-black.jpg", 1),

                (4, "MacBook Air M3 2024 - 16GB/512GB", 32_990_000, null,
                    SeedConstants.CatLaptop, SeedConstants.BrandApple,
                    "CPU: Apple M3 (8-core) | GPU: Apple M3 (10-core) | RAM: 16GB Unified | SSD: 512GB | Màn hình: 13.6\" Liquid Retina | Pin: 52.6Wh",
                    "laptops", "macbook-air-m3-space-gray.jpg", 1),

                (5, "HP Pavilion 15 - Core i5 Gen 13", 13_490_000, 15_990_000,
                    SeedConstants.CatLaptop, SeedConstants.BrandDell,
                    "CPU: Intel Core i5-1335U | RAM: 8GB DDR4 | SSD: 256GB | GPU: Intel UHD | Màn hình: 15.6\" FHD IPS | Pin: 41.6Wh",
                    "laptops", "hp-pavilion-15-blue.jpg", 1),

                (6, "Acer Aspire 5 A515 - Ryzen 5 5500U", 11_990_000, 13_500_000,
                    SeedConstants.CatLaptop, SeedConstants.BrandAcer,
                    "CPU: AMD Ryzen 5 5500U | RAM: 8GB DDR4 | SSD: 512GB PCIe | GPU: Radeon Graphics | Màn hình: 15.6\" FHD IPS | Pin: 50Wh",
                    "laptops", "acer-aspire-5-silver.jpg", 1),

                (7, "ASUS VivoBook Pro 16 - Ryzen 9 5900HX", 22_990_000, 26_000_000,
                    SeedConstants.CatLaptop, SeedConstants.BrandAsus,
                    "CPU: AMD Ryzen 9 5900HX | RAM: 16GB DDR4 | SSD: 1TB PCIe | GPU: RTX 3050 Ti 4GB | Màn hình: 16\" 2.5K 120Hz | Pin: 96Wh",
                    "laptops", "asus-vivobook-14-blue-side.jpg", 1),

                (8, "Lenovo IdeaPad Slim 5i - Core i7 13th", 16_490_000, 18_990_000,
                    SeedConstants.CatLaptop, SeedConstants.BrandLenovo,
                    "CPU: Intel Core i7-1355U | RAM: 16GB DDR5 | SSD: 512GB | GPU: Intel Iris Xe | Màn hình: 14\" 2.8K OLED | Trọng lượng: 1.46kg",
                    "laptops", "lenovo-thinkpad-x1-keyboard.jpg", 1),

                // ── LAPTOP GAMING (11-20) ─────────────────────────────────
                (11, "ASUS ROG Strix G16 2024 - RTX 4090", 54_990_000, 62_000_000,
                    SeedConstants.CatLaptopGaming, SeedConstants.BrandAsus,
                    "CPU: Intel Core i9-14900HX | GPU: RTX 4090 Laptop 16GB | RAM: 32GB DDR5 | SSD: 1TB PCIe 4.0 | Màn hình: 16\" QHD 240Hz | Pin: 90Wh",
                    "laptops-gaming", "asus-rog-strix-g16-black.jpg", 1),

                (12, "MSI Katana 17 B13V - RTX 4070", 29_990_000, 34_000_000,
                    SeedConstants.CatLaptopGaming, SeedConstants.BrandMsi,
                    "CPU: Intel Core i7-13620H | GPU: RTX 4070 Laptop 8GB | RAM: 16GB DDR5 | SSD: 512GB NVMe | Màn hình: 17.3\" FHD 144Hz | Pin: 99Wh",
                    "laptops-gaming", "msi-katana-17-black.jpg", 1),

                (13, "Acer Predator Helios 18 2024 - RTX 4080", 45_990_000, 52_000_000,
                    SeedConstants.CatLaptopGaming, SeedConstants.BrandAcer,
                    "CPU: Intel Core i9-14900HX | GPU: RTX 4080 Laptop 12GB | RAM: 32GB DDR5 | SSD: 1TB PCIe 4.0 | Màn hình: 18\" QHD 250Hz | Pin: 99Wh",
                    "laptops-gaming", "acer-predator-18-black.jpg", 1),

                (14, "Lenovo Legion Pro 7i Gen 9 - RTX 4090", 62_990_000, 72_000_000,
                    SeedConstants.CatLaptopGaming, SeedConstants.BrandLenovo,
                    "CPU: Intel Core i9-14900HX | GPU: RTX 4090 Laptop 16GB | RAM: 32GB DDR5 | SSD: 2TB PCIe 4.0 | Màn hình: 16\" 3.2K OLED 165Hz | Pin: 99.9Wh",
                    "laptops-gaming", "lenovo-legion-9-pro-black.jpg", 1),

                (15, "ASUS TUF Gaming A15 2024 - RTX 4070", 26_990_000, 30_000_000,
                    SeedConstants.CatLaptopGaming, SeedConstants.BrandAsus,
                    "CPU: AMD Ryzen 9 8945H | GPU: RTX 4070 Laptop 8GB | RAM: 16GB DDR5 | SSD: 1TB PCIe 4.0 | Màn hình: 15.6\" FHD 144Hz | Pin: 90Wh",
                    "laptops-gaming", "asus-rog-strix-g16-rgb.jpg", 1),

                (16, "MSI Raider GE78 HX 2024 - RTX 4080", 42_990_000, 48_000_000,
                    SeedConstants.CatLaptopGaming, SeedConstants.BrandMsi,
                    "CPU: Intel Core i9-13980HX | GPU: RTX 4080 Laptop 12GB | RAM: 32GB DDR5 | SSD: 1TB PCIe 5.0 | Màn hình: 17.3\" QHD 240Hz | Pin: 99.9Wh",
                    "laptops-gaming", "msi-katana-17-open.jpg", 1),

                // ── PC GVN (21-26) ────────────────────────────────────────
                (21, "PC Gaming GVN RTX 4060 - Mid Range", 28_990_000, 32_000_000,
                    SeedConstants.CatPcGvn, SeedConstants.BrandAsus,
                    "CPU: Intel Core i5-13600K | GPU: RTX 4060 8GB | RAM: 16GB DDR5 | SSD: 512GB PCIe 4.0 | PSU: 650W 80+ Gold",
                    "pc-builds", "gaming-rtx-4060-front.jpg", 1),

                (22, "PC Gaming GVN RTX 5080 - High End", 68_990_000, 75_000_000,
                    SeedConstants.CatPcGvn, SeedConstants.BrandMsi,
                    "CPU: Intel Core i9-14900K | GPU: RTX 5080 16GB | RAM: 32GB DDR5 6400MHz | SSD: 2TB PCIe 5.0 | PSU: 1000W 80+ Platinum",
                    "pc-builds", "gaming-rtx-5080-front.jpg", 1),

                (23, "PC Gaming GVN RTX 5090 - Extreme", 155_000_000, 170_000_000,
                    SeedConstants.CatPcGvn, SeedConstants.BrandAsus,
                    "CPU: Intel Core i9-14900KS | GPU: RTX 5090 32GB | RAM: 64GB DDR5 7200MHz | SSD: 4TB PCIe 5.0 | PSU: 1200W 80+ Titanium",
                    "pc-builds", "gaming-rtx-5090-front.jpg", 1),

                // ── COMPONENTS (27-36) ────────────────────────────────────
                (27, "ASUS ROG STRIX Z890-E Gaming WiFi", 9_990_000, 11_500_000,
                    SeedConstants.CatMainCpuVga, SeedConstants.BrandAsus,
                    "Socket: LGA1851 | Chipset: Intel Z890 | RAM: DDR5 7600+ | PCIe: 5.0 x16 | WiFi: WiFi 7 | USB 4: Có",
                    "components", "mainboard-asus-rog-z890-front.jpg", 1),

                (28, "Intel Core Ultra 9 285K - 24 Cores", 14_990_000, 17_000_000,
                    SeedConstants.CatMainCpuVga, SeedConstants.BrandIntel,
                    "Cores/Threads: 24C/24T | Frequency: 5.7GHz Max | Cache: 36MB L3 | Socket: LGA1851 | TDP: 125W | Arch: Arrow Lake",
                    "components", "cpu-intel-i9-14900k-box.jpg", 1),

                (29, "NVIDIA RTX 5090 - 32GB GDDR7 Founders", 92_990_000, 99_000_000,
                    SeedConstants.CatMainCpuVga, SeedConstants.BrandNvidia,
                    "VRAM: 32GB GDDR7 | CUDA Cores: 21760 | Memory Bus: 576-bit | TDP: 575W | PCIe: 5.0 | DLSS 4: Có",
                    "components", "gpu-rtx-5090-front.jpg", 1),

                (30, "NVIDIA RTX 5080 - 16GB GDDR7", 49_990_000, 55_000_000,
                    SeedConstants.CatMainCpuVga, SeedConstants.BrandNvidia,
                    "VRAM: 16GB GDDR7 | CUDA Cores: 10752 | Memory Bus: 256-bit | TDP: 360W | PCIe: 5.0 | DLSS 4: Có",
                    "components", "gpu-rtx-5080-front.jpg", 1),

                (31, "Samsung 990 Pro NVMe 2TB", 4_990_000, 5_990_000,
                    SeedConstants.CatStorageRam, SeedConstants.BrandSamsung,
                    "Dung lượng: 2TB | Interface: PCIe 4.0 NVMe | Read: 7450 MB/s | Write: 6900 MB/s | Endurance: 1200 TBW",
                    "storage", "ssd-samsung-990-pro-box.jpg", 1),

                (32, "Corsair Dominator Titanium 32GB DDR5", 4_490_000, 5_200_000,
                    SeedConstants.CatStorageRam, SeedConstants.BrandCorsair,
                    "Dung lượng: 32GB (2x16GB) | Speed: 6000MHz | CAS: CL30 | Type: DDR5 | Voltage: 1.35V | RGB: Có",
                    "storage", "ram-corsair-dominator-box.jpg", 1),

                (33, "Kingston FURY Beast 64GB DDR5 6000MHz", 8_990_000, 10_500_000,
                    SeedConstants.CatStorageRam, SeedConstants.BrandKingston,
                    "Dung lượng: 64GB (2x32GB) | Speed: 6000MHz | CAS: CL36 | Type: DDR5 | Voltage: 1.4V | Intel XMP: Có",
                    "storage", "ram-kingston-fury-box.jpg", 1),

                // ── PERIPHERALS (37-50) ───────────────────────────────────
                (37, "Logitech G Pro X Superlight 2 DEX", 2_990_000, 3_490_000,
                    SeedConstants.CatChuot, SeedConstants.BrandLogitech,
                    "Weight: 60g | DPI: 32000 | Polling: 8000Hz | Battery: 95h | Sensor: HERO 2 | Connection: Wireless",
                    "peripherals", "mouse-logitech-mx-master-front.jpg", 1),

                (38, "Razer Viper V3 Pro - 30K DPI", 2_490_000, 2_990_000,
                    SeedConstants.CatChuot, SeedConstants.BrandRazer,
                    "Weight: 54g | DPI: 30000 | Polling: 8000Hz | Battery: 80h | Sensor: Focus Pro | Connection: Wireless",
                    "peripherals", "mouse-razer-deathadder-v3-front.jpg", 1),

                (39, "AKKO MOD007B PC HE - Magnetic Switch", 2_790_000, 3_290_000,
                    SeedConstants.CatBanPhim, SeedConstants.BrandAkko,
                    "Layout: 75% | Switch: Gateron Magnetic White | Hot-swap: Có | RGB: Per-key | Case: Polycarbonate | Gasket: Có",
                    "peripherals", "keyboard-akko-3098b-front.jpg", 1),

                (40, "Corsair K100 Air Wireless - Cherry MX", 5_490_000, 6_500_000,
                    SeedConstants.CatBanPhim, SeedConstants.BrandCorsair,
                    "Layout: Full Size | Switch: Cherry MX Speed | Hot-swap: Không | RGB: Per-key | Battery: 200h | Connection: 2.4GHz",
                    "peripherals", "keyboard-corsair-k95-front.jpg", 1),

                (41, "HyperX Cloud III Wireless - 7.1 Surround", 2_790_000, 3_290_000,
                    SeedConstants.CatTaiNghe, SeedConstants.BrandHyperX,
                    "Driver: 53mm | Frequency: 10-21000Hz | Battery: 120h | Connection: 2.4GHz | Surround: 7.1 | Mic: Detachable",
                    "peripherals", "headset-hyperx-cloud-front.jpg", 1),

                (42, "SteelSeries Arctis Nova Pro Wireless", 6_990_000, 7_990_000,
                    SeedConstants.CatTaiNghe, SeedConstants.BrandSteelSeries,
                    "Driver: 40mm Neodymium | Frequency: 10-40000Hz | Battery: Dual | Connection: 2.4GHz + BT | Mic: ClearCast V2 | DAC: Kèm theo",
                    "peripherals", "headset-corsair-virtuoso-front.jpg", 1),

                // ── MONITORS (61-70) ──────────────────────────────────────
                (61, "LG UltraGear 27GP850-B - 27\" 1440p 165Hz", 9_490_000, 10_990_000,
                    SeedConstants.CatManHinh, SeedConstants.BrandLg,
                    "Panel: IPS 27\" | Resolution: 2560x1440 | Refresh: 165Hz | Response: 1ms GtG | HDR: HDR400 | G-Sync Compatible",
                    "monitor", "lg-27gp850-front.jpg", 1),

                (62, "ASUS ROG Swift Pro PG248QP - 540Hz", 14_990_000, 17_000_000,
                    SeedConstants.CatManHinh, SeedConstants.BrandAsus,
                    "Panel: TN 24.1\" | Resolution: 1920x1080 | Refresh: 540Hz | Response: 0.2ms | G-Sync: Native | Đặc biệt: Esports chuyên nghiệp",
                    "monitor", "asus-pa347cv-front.jpg", 1),

                (63, "Samsung Odyssey G9 Neo 2024 - 57\" DQHD 240Hz", 38_990_000, 45_000_000,
                    SeedConstants.CatManHinh, SeedConstants.BrandSamsung,
                    "Panel: Mini-LED 57\" Curved | Resolution: 7680x2160 DQHD | Refresh: 240Hz | Response: 1ms | HDR: HDR2000 | FreeSync: Premium Pro",
                    "monitor", "asus-proart-32-front.jpg", 1),

                (64, "BenQ MOBIUZ EX2710U - 27\" 4K 144Hz", 14_490_000, 16_500_000,
                    SeedConstants.CatManHinh, SeedConstants.BrandBenq,
                    "Panel: IPS 27\" | Resolution: 3840x2160 4K | Refresh: 144Hz | Response: 1ms GtG | HDR: HDR600 | USB-C: 65W PD",
                    "monitor", "lg-27gp850-stand.jpg", 1),
            };

            foreach (var (id, name, price, listPrice, catId, brandId, specs, imgFolder, imgFile, status) in products)
            {
                var p = new Product
                {
                    Id = id,
                    Name = name,
                    Price = price,
                    ListPrice = listPrice,
                    CategoryId = catId,
                    BrandId = brandId,
                    Status = status,
                    Specifications = specs,
                    WarrantyInfo = "24 tháng chính hãng",
                    DiscountNote = listPrice.HasValue ? "Giảm giá | Tặng túi chống sốc | Vệ sinh miễn phí" : "Giao hàng nhanh | Vệ sinh miễn phí",
                    Description = BuildDescription(name),
                    CreatedAt = now.AddDays(-rng.Next(1, 90))
                };
                context.Products.Add(p);

                context.ProductImages.Add(new ProductImage
                {
                    ProductId = id,
                    ImageUrl = $"{imgFolder}/{imgFile}",
                    IsDefault = true,
                    CreatedAt = now
                });

                context.Inventories.Add(new Inventory
                {
                    ProductId = id,
                    Quantity = rng.Next(5, 50),
                    UpdatedAt = now
                });
            }

            await context.SaveChangesAsync();

                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Products] OFF");
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        private static string BuildDescription(string name) =>
            $"<h5 class='text-danger fw-bold'>Đặc điểm nổi bật</h5>" +
            $"<ul><li>✓ {name} – sản phẩm chính hãng, bảo hành 24 tháng</li>" +
            $"<li>✓ Giao hàng toàn quốc, COD tại Đà Nẵng</li>" +
            $"<li>✓ Hỗ trợ trả góp 0% lãi suất qua thẻ tín dụng</li></ul>";
    }
}
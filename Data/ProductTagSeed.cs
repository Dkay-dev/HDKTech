// Data/ProductTagSeed.cs
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    public static class ProductTagSeed
    {
        public static async Task SeedAsync(HDKTechContext context)
        {
            // ── Guard: không seed nếu đã có data ────────────────────────
            if (await context.ProductTags.AnyAsync()) return;

            // ── Validate: đảm bảo products đã có trước khi seed tags ────
            var productCount = await context.Products.CountAsync();
            if (productCount == 0)
            {
                throw new InvalidOperationException(
                    "ProductTagSeed: Products phải được seed trước. " +
                    "Kiểm tra thứ tự seed trong DbInitializer.");
            }

            var tags = new List<ProductTag>();

            // ── Helper local function ────────────────────────────────────
            void AddTag(int productId, string key, string value)
                => tags.Add(new ProductTag
                {
                    ProductId = productId,
                    TagKey = key,
                    TagValue = value
                });

            // ── Laptop (IDs 1-8) ─────────────────────────────────────────
            AddTag(1, "CPU", "Core i7-1360P");
            AddTag(1, "RAM", "16GB");
            AddTag(1, "Storage", "512GB SSD");
            AddTag(1, "Screen", "13.4 inch");

            AddTag(2, "CPU", "Ryzen 7 5700U");
            AddTag(2, "RAM", "16GB");
            AddTag(2, "Storage", "512GB SSD");
            AddTag(2, "Screen", "15.6 inch");

            AddTag(3, "CPU", "Core Ultra 7 155H");
            AddTag(3, "RAM", "16GB");
            AddTag(3, "Storage", "512GB SSD");
            AddTag(3, "Screen", "14 inch");

            AddTag(4, "CPU", "Apple M3");
            AddTag(4, "RAM", "16GB");
            AddTag(4, "Storage", "512GB SSD");
            AddTag(4, "Screen", "13.6 inch");

            AddTag(5, "CPU", "Core i5-1335U");
            AddTag(5, "RAM", "8GB");
            AddTag(5, "Storage", "256GB SSD");
            AddTag(5, "Screen", "15.6 inch");

            AddTag(6, "CPU", "Ryzen 5 5500U");
            AddTag(6, "RAM", "8GB");
            AddTag(6, "Storage", "512GB SSD");
            AddTag(6, "Screen", "15.6 inch");

            AddTag(7, "CPU", "Ryzen 9 5900HX");
            AddTag(7, "RAM", "16GB");
            AddTag(7, "VGA", "RTX 3050 Ti");
            AddTag(7, "Storage", "1TB SSD");
            AddTag(7, "Screen", "16 inch");

            AddTag(8, "CPU", "Core i7-1355U");
            AddTag(8, "RAM", "16GB");
            AddTag(8, "Storage", "512GB SSD");
            AddTag(8, "Screen", "14 inch");

            // ── Laptop Gaming (IDs 11-16) ────────────────────────────────
            AddTag(11, "CPU", "Core i9-14900HX");
            AddTag(11, "VGA", "RTX 4090");
            AddTag(11, "RAM", "32GB");
            AddTag(11, "Storage", "1TB SSD");
            AddTag(11, "Screen", "16 inch");

            AddTag(12, "CPU", "Core i7-13620H");
            AddTag(12, "VGA", "RTX 4070");
            AddTag(12, "RAM", "16GB");
            AddTag(12, "Storage", "512GB SSD");
            AddTag(12, "Screen", "17.3 inch");

            AddTag(13, "CPU", "Core i9-14900HX");
            AddTag(13, "VGA", "RTX 4080");
            AddTag(13, "RAM", "32GB");
            AddTag(13, "Storage", "1TB SSD");
            AddTag(13, "Screen", "18 inch");

            AddTag(14, "CPU", "Core i9-14900HX");
            AddTag(14, "VGA", "RTX 4090");
            AddTag(14, "RAM", "32GB");
            AddTag(14, "Storage", "2TB SSD");
            AddTag(14, "Screen", "16 inch");

            AddTag(15, "CPU", "Ryzen 9 8945H");
            AddTag(15, "VGA", "RTX 4070");
            AddTag(15, "RAM", "16GB");
            AddTag(15, "Storage", "1TB SSD");
            AddTag(15, "Screen", "15.6 inch");

            AddTag(16, "CPU", "Core i9-13980HX");
            AddTag(16, "VGA", "RTX 4080");
            AddTag(16, "RAM", "32GB");
            AddTag(16, "Storage", "1TB SSD");
            AddTag(16, "Screen", "17.3 inch");

            // ── Components (IDs 29-33) ───────────────────────────────────
            AddTag(29, "VGA", "RTX 5090");
            AddTag(29, "VRAM", "32GB");

            AddTag(30, "VGA", "RTX 5080");
            AddTag(30, "VRAM", "16GB");

            AddTag(31, "Storage", "2TB SSD");
            AddTag(31, "Type", "NVMe PCIe 4.0");

            AddTag(32, "RAM", "32GB");
            AddTag(32, "Speed", "6000MHz DDR5");

            AddTag(33, "RAM", "64GB");
            AddTag(33, "Speed", "6000MHz DDR5");

            await context.ProductTags.AddRangeAsync(tags);
            await context.SaveChangesAsync();

            // ── Post-seed validation ─────────────────────────────────────
            var seededCount = await context.ProductTags.CountAsync();
            if (seededCount != tags.Count)
            {
                throw new Exception(
                    $"ProductTagSeed: Expected {tags.Count} tags, " +
                    $"but found {seededCount} in DB.");
            }
        }
    }
}
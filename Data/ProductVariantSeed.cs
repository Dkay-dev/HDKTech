using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    /// <summary>
    /// Seed ProductVariant (mỗi Product cũ → 1 variant mặc định) và Inventory
    /// tương ứng. Hai entity này coupled chặt trong refactor mới:
    ///   Product (metadata) ── 1-n ─▶ ProductVariant (SKU/giá) ── 1-n ─▶ Inventory.
    ///
    /// Nguồn dữ liệu giá / specs: <see cref="ProductSeed.Rows"/> (single source of truth).
    /// </summary>
    public static class ProductVariantSeed
    {
        public static async Task SeedAsync(HDKTechContext context)
        {
            if (await context.ProductVariants.AnyAsync()) return;

            using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.Now;
                var rng = new Random(42);   // deterministic như seed cũ

                // ── 1. Variants ─────────────────────────────────────
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [ProductVariants] ON");

                var variants = new List<ProductVariant>(ProductSeed.Rows.Length);
                var skuSet   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var r in ProductSeed.Rows)
                {
                    var specs = ParseSpecs(r.Specs);
                    var sku   = GenerateSku(r.Name, r.Id, skuSet);

                    variants.Add(new ProductVariant
                    {
                        Id          = SeedConstants.DefaultVariantId(r.Id),
                        ProductId   = r.Id,
                        Sku         = sku,
                        VariantName = BuildVariantName(specs),
                        Cpu         = specs.Cpu,
                        Ram         = specs.Ram,
                        Storage     = specs.Storage,
                        Gpu         = specs.Gpu,
                        Screen      = specs.Screen,
                        Color       = null,
                        Os          = null,
                        Price       = r.Price,
                        ListPrice   = r.ListPrice,
                        CostPrice   = Math.Round(r.Price * 0.78m, 0),   // ước lượng COGS 78%
                        IsActive    = true,
                        IsDefault   = true,
                        CreatedAt   = now
                    });
                }

                await context.ProductVariants.AddRangeAsync(variants);
                await context.SaveChangesAsync();
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [ProductVariants] OFF");

                // ── 2. Inventory (PK auto, FK → ProductVariantId) ───
                var inventories = variants.Select(v => new Inventory
                {
                    ProductId         = v.ProductId,
                    ProductVariantId  = v.Id,
                    WarehouseId       = null,                       // single-warehouse tạm thời
                    Quantity          = rng.Next(5, 50),            // giữ đúng distribution cũ
                    ReservedQuantity  = 0,
                    LowStockThreshold = 5,
                    UpdatedAt         = now
                }).ToList();

                await context.Inventories.AddRangeAsync(inventories);
                await context.SaveChangesAsync();

                // ── 3. Initial StockMovement (nhập kho đầu kỳ) ──────
                var movements = inventories.Select(inv => new StockMovement
                {
                    InventoryId   = inv.Id,
                    Quantity      = inv.Quantity,
                    Reason        = StockMovementReason.Restock,
                    ReferenceType = "Seed",
                    Note          = "Nhập kho đầu kỳ (seed)",
                    CreatedBy     = SeedConstants.AdminUserId,
                    CreatedAt     = now
                }).ToList();

                await context.StockMovements.AddRangeAsync(movements);
                await context.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ── Helpers ────────────────────────────────────────────────

        /// <summary>Các trường tách được từ chuỗi Specs (vd "CPU: ... | RAM: ...").</summary>
        private record SpecBreakdown(
            string? Cpu, string? Ram, string? Storage, string? Gpu, string? Screen);

        /// <summary>Parse chuỗi "Key: Value | Key: Value …" thành SpecBreakdown.</summary>
        private static SpecBreakdown ParseSpecs(string raw)
        {
            string? cpu = null, ram = null, storage = null, gpu = null, screen = null;

            foreach (var part in raw.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split(':', 2);
                if (kv.Length != 2) continue;
                var key = kv[0].Trim();
                var val = kv[1].Trim();

                if (key.Equals("CPU",        StringComparison.OrdinalIgnoreCase)) cpu     = Trunc(val, 100);
                else if (key.Equals("RAM",   StringComparison.OrdinalIgnoreCase)
                      || key.Equals("Dung lượng", StringComparison.OrdinalIgnoreCase)) ram = Trunc(val, 50);
                else if (key.Equals("SSD",   StringComparison.OrdinalIgnoreCase)
                      || key.Equals("Storage", StringComparison.OrdinalIgnoreCase)) storage = Trunc(val, 50);
                else if (key.Equals("GPU",   StringComparison.OrdinalIgnoreCase)
                      || key.Equals("VRAM",  StringComparison.OrdinalIgnoreCase)) gpu     = Trunc(val, 100);
                else if (key.Equals("Màn hình", StringComparison.OrdinalIgnoreCase)
                      || key.Equals("Panel", StringComparison.OrdinalIgnoreCase)
                      || key.Equals("Resolution", StringComparison.OrdinalIgnoreCase)) screen = Trunc(val, 50);
            }
            return new SpecBreakdown(cpu, ram, storage, gpu, screen);
        }

        private static string? Trunc(string? s, int max) =>
            string.IsNullOrWhiteSpace(s) ? null : (s.Length <= max ? s : s[..max]);

        /// <summary>Gộp gọn specs chính thành tên biến thể (fallback "Bản tiêu chuẩn").</summary>
        private static string BuildVariantName(SpecBreakdown s)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(s.Ram))     parts.Add(s.Ram!);
            if (!string.IsNullOrWhiteSpace(s.Storage)) parts.Add(s.Storage!);
            return parts.Count == 0 ? "Bản tiêu chuẩn" : string.Join(" / ", parts);
        }

        /// <summary>
        /// Sinh SKU từ tên sản phẩm: bỏ dấu → UPPER → lấy 4 ký tự đầu của 3 token đầu
        /// → append "-{id:D3}". Bảo đảm duy nhất bằng HashSet (append '-N' nếu va chạm).
        /// </summary>
        private static string GenerateSku(string name, int productId, HashSet<string> used)
        {
            var normalized = name
                .Normalize(NormalizationForm.FormD)
                .Replace("đ", "d")
                .Replace("Đ", "D");
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(ch);
            }
            var ascii = Regex.Replace(sb.ToString().ToUpperInvariant(), "[^A-Z0-9 ]", " ");
            var tokens = ascii.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Lấy 3 token đầu, mỗi token tối đa 4 ký tự
            var head = string.Join("-", tokens.Take(3).Select(t => t.Length > 4 ? t[..4] : t));
            if (string.IsNullOrWhiteSpace(head)) head = "PROD";

            var baseSku = $"{head}-{productId:D3}";
            if (baseSku.Length > 60) baseSku = baseSku[..60];

            var sku = baseSku;
            var dup = 1;
            while (!used.Add(sku))
            {
                sku = $"{baseSku}-{dup++}";
            }
            return sku;
        }
    }
}

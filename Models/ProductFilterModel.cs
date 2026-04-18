// Models/ProductFilterModel.cs — thay thế hoàn toàn
namespace HDKTech.Models
{
    /// <summary>
    /// Filter model chuẩn hóa: dùng BrandIds (int) thay vì BrandNames (string).
    /// Tránh GetHashCode bug và chuẩn hơn cho query.
    /// </summary>
    public class ProductFilterModel
    {
        public int? CategoryId { get; set; }

        // ✅ FIX: Dùng BrandId thay BrandName — tránh GetHashCode collision
        public List<int>? BrandIds { get; set; }

        // Giữ lại để backward compat với URL params (parse từ "Dell,ASUS" → IDs)
        [System.Text.Json.Serialization.JsonIgnore]
        public List<string>? BrandNames { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? Status { get; set; }
        public string SortBy { get; set; } = "featured";
        public string? SearchKeyword { get; set; }

        // ── Spec filters (dùng ProductTag) ──────────────────────────────
        public string? RamFilter { get; set; }   // e.g. "16GB"
        public string? CpuFilter { get; set; }   // e.g. "Core i7"
        public string? VgaFilter { get; set; }   // e.g. "RTX 4070"

        // ── Pagination ───────────────────────────────────────────────────
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
    }

    /// <summary>
    /// DTO chứa filter options động — tính toán dựa trên kết quả filter hiện tại.
    /// </summary>
    public class FilterOptions
    {
        public List<BrandOption> AvailableBrands { get; set; } = new();
        public List<string> AvailableCpus { get; set; } = new();
        public List<string> AvailableVgas { get; set; } = new();
        public List<string> AvailableRams { get; set; } = new();
        public decimal MinPriceInSet { get; set; }
        public decimal MaxPriceInSet { get; set; }
    }

    public class BrandOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>Kết quả filter + pagination + options động.</summary>
    public class ProductFilterResult
    {
        public List<Product> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public FilterOptions Options { get; set; } = new();
    }
}
namespace HDKTech.Models
{
    public class ProductFilterModel
    {
        public int? CategoryId { get; set; }

        // FIX: Filter brands by NAME (string), not by hash/int.
        // Populated from "brandNames=Dell,ASUS,Lenovo" query-string.
        public List<string>? BrandNames { get; set; }

        // Keep single BrandId for backwards compatibility with any old code
        public int? BrandId { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? Status { get; set; }          // 1: Còn hàng, 0: Hết hàng
        public string? SortBy { get; set; }
        public string? SearchKeyword { get; set; }

        // Laptop specs
        public string? CpuLine { get; set; }
        public string? VgaLine { get; set; }
        public string? RamType { get; set; }
    }
}
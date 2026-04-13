namespace HDKTech.Models
{
    public class ProductFilterModel
    {
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? Status { get; set; } // 1: Có hàng, 0: Hết hàng
        public string? SortBy { get; set; } // featured, name_asc, name_desc, price_asc, price_desc, new
        public string? SearchKeyword { get; set; }
        
        // Đặc biệt cho laptop
        public string? CpuLine { get; set; } // i3, i5, i7, i9, Ryzen 5, etc.
        public string? VgaLine { get; set; } // RTX 4050, RTX 4060, etc.
        public string? RamType { get; set; } // DDR4, DDR5, etc.
    }
}

using HDKTech.Areas.Admin.Models;
using HDKTech.Models;
using Microsoft.AspNetCore.Http;

namespace HDKTech.Areas.Admin.Services.Interfaces
{
    public interface IProductAdminService
    {
        // Listing + search/filter với pagination
        Task<(List<Product> Products, int TotalCount, List<Category> Categories, List<Brand> Brands)>
            GetProductsPagedAsync(string searchTerm, int? categoryId, int? brandId, int page, int pageSize);

        // Dropdowns cho Create/Edit form
        Task<(List<Category> Categories, List<Brand> Brands, List<WarrantyPolicy> WarrantyPolicies)>
            GetDropdownsAsync();

        // Flash sale lookup
        Task<Promotion?> GetFlashSaleForProductAsync(int productId);

        // Flash sale create/update/deactivate
        Task SaveFlashSaleAsync(int productId, string productName, bool isFlashSale,
            decimal flashSalePrice, DateTime? flashSaleStart, DateTime? flashSaleEnd);

        // Variant + inventory
        Task<ProductVariant> CreateDefaultVariantAsync(int productId, ProductVariant variant, int initialStock);
        Task UpdateDefaultVariantAsync(Product existingProduct, ProductVariant variant);

        // Images
        Task SaveProductImagesAsync(int productId, IList<IFormFile> files, string? categoryName);
        Task<string?> DeleteImageAsync(int imageId);
        Task<bool> SetDefaultImageAsync(int imageId);

        // Product CRUD (delegates to IAdminProductRepository)
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product?> GetProductWithVariantsAsync(int id);
        Task<Product> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<(bool Success, string? Error, IList<string> ImageUrls)> DeleteProductAsync(int id, string deletedBy);
        Task<bool> UpdateVariantStockAsync(int variantId, int quantity);
    }
}

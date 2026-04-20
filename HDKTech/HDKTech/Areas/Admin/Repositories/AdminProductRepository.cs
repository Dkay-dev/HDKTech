using HDKTech.Models;
using HDKTech.Data;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Repositories
{
    /// <summary>
    /// Admin Product Repository — refactor:
    ///  - KHÔNG còn Product.Price / ListPrice / FlashSale* — các thao tác giá
    ///    được chuyển sang ProductVariant qua UpdateVariantPriceAsync.
    ///  - Stock thao tác qua Inventory.ProductVariantId (không còn ProductId PK).
    /// </summary>
    public class AdminProductRepository : IAdminProductRepository
    {
        private readonly HDKTechContext _context;
        private readonly ILogger<AdminProductRepository> _logger;

        public AdminProductRepository(HDKTechContext context, ILogger<AdminProductRepository> logger)
        {
            _context = context;
            _logger  = logger;
        }

        #region Read

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                return new List<Product>();
            }
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Variants).ThenInclude(v => v.Inventories)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by id: {ProductId}", id);
                return null;
            }
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            try
            {
                return await _context.Products
                    .Where(p => p.CategoryId == categoryId)
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Variants)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by category: {CategoryId}", categoryId);
                return new List<Product>();
            }
        }

        public async Task<IEnumerable<Product>> GetProductsByBrandAsync(int brandId)
        {
            try
            {
                return await _context.Products
                    .Where(p => p.BrandId == brandId)
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Variants)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by brand: {BrandId}", brandId);
                return new List<Product>();
            }
        }

        public async Task<(IEnumerable<Product> products, int totalCount)> GetProductsPagedAsync(int pageNumber, int pageSize)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Variants);

                var totalCount = await query.CountAsync();
                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (products, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged products");
                return (new List<Product>(), 0);
            }
        }

        #endregion

        #region Create

        public async Task<Product> CreateProductAsync(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product created: {ProductName}", product.Name);
            return product;
        }

        #endregion

        #region Update

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                if (product == null) throw new ArgumentNullException(nameof(product));

                var existing = await _context.Products.FindAsync(product.Id);
                if (existing == null)
                {
                    _logger.LogWarning("Product not found for update: {ProductId}", product.Id);
                    return false;
                }

                existing.Name             = product.Name;
                existing.Slug             = product.Slug;
                existing.Description      = product.Description;
                existing.CategoryId       = product.CategoryId;
                existing.BrandId          = product.BrandId;
                existing.WarrantyPolicyId = product.WarrantyPolicyId;
                existing.Status           = product.Status;
                existing.Specifications   = product.Specifications;
                existing.UpdatedAt        = DateTime.Now;

                _context.Products.Update(existing);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product updated: {ProductName}", product.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", product?.Id);
                return false;
            }
        }

        public async Task<bool> UpdateVariantStockAsync(int productVariantId, int quantity)
        {
            try
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductVariantId == productVariantId);

                if (inventory == null)
                {
                    var variant = await _context.ProductVariants
                        .FirstOrDefaultAsync(v => v.Id == productVariantId);
                    if (variant == null)
                    {
                        _logger.LogWarning("Variant not found for stock update: {VariantId}", productVariantId);
                        return false;
                    }

                    inventory = new Inventory
                    {
                        ProductVariantId = productVariantId,
                        ProductId        = variant.ProductId,
                        Quantity         = quantity,
                        UpdatedAt        = DateTime.Now
                    };
                    _context.Inventories.Add(inventory);
                }
                else
                {
                    inventory.Quantity  = quantity;
                    inventory.UpdatedAt = DateTime.Now;
                    _context.Inventories.Update(inventory);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Variant stock updated: {VariantId} → {Qty}", productVariantId, quantity);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating variant stock: {VariantId}", productVariantId);
                return false;
            }
        }

        public async Task<bool> UpdateVariantPriceAsync(int productVariantId, decimal price)
        {
            try
            {
                var variant = await _context.ProductVariants.FindAsync(productVariantId);
                if (variant == null)
                {
                    _logger.LogWarning("Variant not found for price update: {VariantId}", productVariantId);
                    return false;
                }

                variant.Price     = price;
                variant.UpdatedAt = DateTime.Now;
                _context.ProductVariants.Update(variant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Variant price updated: {VariantId} → {Price}", productVariantId, price);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating variant price: {VariantId}", productVariantId);
                return false;
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Module C — Soft Delete: đánh dấu IsDeleted=true + ghi DeletedAt/DeletedBy.
        /// Image files vẫn được trả về để controller xóa file vật lý nếu muốn.
        /// Record không bị xóa khỏi DB → lịch sử OrderItem được bảo toàn.
        /// </summary>
        public async Task<(bool success, string? error, IList<string> imageUrls)> DeleteProductAsync(
            int id, string? deletedBy = null)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                    return (false, "Product does not exist.", new List<string>());

                var hasActiveOrders = await _context.OrderItems
                    .AnyAsync(ct => ct.ProductId == id
                        && ct.Order != null
                        && (int)ct.Order.Status < 4);

                if (hasActiveOrders)
                    return (false,
                        "Cannot delete: product đang có đơn hàng chưa hoàn tất. Hãy vô hiệu hoá thay vì xóa.",
                        new List<string>());

                var imageUrls = product.Images?
                    .Select(h => h.ImageUrl)
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Select(u => u!)
                    .ToList() ?? new List<string>();

                // Soft delete — giữ record để bảo toàn FK từ OrderItem
                product.IsDeleted = true;
                product.DeletedAt = DateTime.Now;
                product.DeletedBy = deletedBy;
                product.UpdatedAt = DateTime.Now;

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Product {ProductId} soft-deleted by {User}. {Count} images to clean up.",
                    id, deletedBy ?? "unknown", imageUrls.Count);
                return (true, null, imageUrls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting product: {ProductId}", id);
                return (false, "System error while deleting product.", new List<string>());
            }
        }

        public async Task<(int deleted, int skipped, IList<string> imageUrls)> DeleteProductsAsync(
            IEnumerable<int> ids, string? deletedBy = null)
        {
            var allImageUrls = new List<string>();
            int deleted = 0, skipped = 0;

            foreach (var id in ids.ToList())
            {
                var (success, _, imageUrls) = await DeleteProductAsync(id, deletedBy);
                if (success) { deleted++; allImageUrls.AddRange(imageUrls); }
                else skipped++;
            }

            return (deleted, skipped, allImageUrls);
        }

        #endregion

        #region Search and Filter

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllProductsAsync();

                var kw = searchTerm.ToLower();
                return await _context.Products
                    .Where(p =>
                        p.Name.ToLower().Contains(kw) ||
                        (p.Description != null && p.Description.ToLower().Contains(kw)))
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Variants)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products: {SearchTerm}", searchTerm);
                return new List<Product>();
            }
        }

        public async Task<IEnumerable<Product>> FilterProductsAsync(ProductFilterCriteria criteria)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Variants).ThenInclude(v => v.Inventories)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
                {
                    var kw = criteria.SearchTerm.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(kw) ||
                        (p.Description != null && p.Description.ToLower().Contains(kw)));
                }

                if (criteria.CategoryId.HasValue)
                    query = query.Where(p => p.CategoryId == criteria.CategoryId.Value);

                if (criteria.BrandId.HasValue)
                    query = query.Where(p => p.BrandId == criteria.BrandId.Value);

                if (criteria.MinPrice.HasValue)
                    query = query.Where(p => p.Variants.Any(v => v.IsActive && v.Price >= criteria.MinPrice.Value));

                if (criteria.MaxPrice.HasValue)
                    query = query.Where(p => p.Variants.Any(v => v.IsActive && v.Price <= criteria.MaxPrice.Value));

                if (criteria.InStock == true)
                    query = query.Where(p =>
                        p.Variants.Any(v => v.Inventories.Any(i => i.Quantity - i.ReservedQuantity > 0)));

                if (criteria.IsActive == true)
                    query = query.Where(p => p.Status == 1);

                return await query.OrderBy(p => p.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering products");
                return new List<Product>();
            }
        }

        #endregion

        #region Exists

        public async Task<bool> ProductExistsAsync(int id)
        {
            try
            {
                return await _context.Products.AnyAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking product existence: {ProductId}", id);
                return false;
            }
        }

        public async Task<bool> CheckSkuExistsAsync(string sku, int? excludeVariantId = null)
        {
            if (string.IsNullOrWhiteSpace(sku)) return false;
            try
            {
                var q = _context.ProductVariants.Where(v => v.Sku == sku);
                if (excludeVariantId.HasValue)
                    q = q.Where(v => v.Id != excludeVariantId.Value);
                return await q.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking SKU: {SKU}", sku);
                return false;
            }
        }

        #endregion
    }
}

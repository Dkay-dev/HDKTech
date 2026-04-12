using HDKTech.Models;
using HDKTech.Data;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HDKTech.Areas.Admin.Repositories
{
    /// <summary>
    /// Admin Product Repository - Handles all product CRUD operations
    /// Uses Entity Framework Core for data access
    /// </summary>
    public class AdminProductRepository : IAdminProductRepository
    {
        private readonly HDKTechContext _context;
        private readonly ILogger<AdminProductRepository> _logger;

        public AdminProductRepository(HDKTechContext context, ILogger<AdminProductRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Read Operations

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                return new List<Product>();
            }
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Inventories)
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
                    .Include(p => p.Images);

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

        #region Create Operations

        public async Task<Product> CreateProductAsync(Product product)
        {
            try
            {
                if (product == null)
                {
                    throw new ArgumentNullException(nameof(product));
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product created successfully: {ProductName}", product.Name);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        #endregion

        #region Update Operations

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                if (product == null)
                {
                    throw new ArgumentNullException(nameof(product));
                }

                var existingProduct = await _context.Products.FindAsync(product.Id);
                if (existingProduct == null)
                {
                    _logger.LogWarning("Product not found for update: {ProductId}", product.Id);
                    return false;
                }

                // Update properties
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.BrandId = product.BrandId;
                existingProduct.Status = product.Status;
                existingProduct.Specifications = product.Specifications;
                existingProduct.WarrantyInfo = product.WarrantyInfo;
                existingProduct.DiscountNote = product.DiscountNote;
                existingProduct.ListPrice = product.ListPrice;

                _context.Products.Update(existingProduct);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product updated successfully: {ProductName}", product.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", product.Id);
                return false;
            }
        }

        public async Task<bool> UpdateProductStockAsync(int productId, int quantity)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for stock update: {ProductId}", productId);
                    return false;
                }

                // Update stock in Inventory table (inventory is managed separately)
                var inventory = await _context.Inventories.FirstOrDefaultAsync(k => k.ProductId == productId);
                if (inventory != null)
                {
                    inventory.Quantity = quantity;
                    inventory.UpdatedAt = DateTime.Now;
                    _context.Inventories.Update(inventory);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Product stock updated: {ProductId}, Quantity: {Quantity}", productId, quantity);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product stock: {ProductId}", productId);
                return false;
            }
        }

        public async Task<bool> UpdateProductPriceAsync(int productId, decimal price)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for price update: {ProductId}", productId);
                    return false;
                }

                product.Price = price;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product price updated: {ProductId}, Price: {Price}", productId, price);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product price: {ProductId}", productId);
                return false;
            }
        }

        #endregion

        #region Delete Operations

        /// <summary>
        /// Safe product deletion:
        /// 1. Blocks deletion if product has unfinished orders (Pending/Processing/Shipping)
        /// 2. Returns image URLs so controller can delete physical files
        /// </summary>
        public async Task<(bool success, string error, IList<string> imageUrls)> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    _logger.LogWarning("Product not found for deletion: {ProductId}", id);
                    return (false, "Product does not exist.", new List<string>());
                }

                // ── Check for active orders ───────────────────────
                // Status 0, 1, 2 = Pending/Processing/Shipping — product CANNOT be deleted
                var hasActiveOrders = await _context.OrderItems
                    .AnyAsync(ct => ct.ProductId == id
                        && ct.Order.Status < 3); // 0,1,2 = not yet completed

                if (hasActiveOrders)
                    return (false,
                        "Cannot delete: product is part of an unfinished order. Disable the product instead of deleting it.",
                        new List<string>());

                // ── Collect image URLs to delete files after DB commit ─────
                var imageUrls = product.Images?.Select(h => h.ImageUrl).ToList() ?? new List<string>();

                _context.Products.Remove(product); // Cascade deletes Images + Inventory in DB
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product {ProductId} deleted. {Count} images to clean up.", id, imageUrls.Count);
                return (true, null, imageUrls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                return (false, "System error while deleting product.", new List<string>());
            }
        }

        /// <summary>
        /// Safe bulk product deletion — skips products with active orders.
        /// Returns list of image URLs that need file cleanup.
        /// </summary>
        public async Task<(int deleted, int skipped, IList<string> imageUrls)> DeleteProductsAsync(IEnumerable<int> ids)
        {
            var idList = ids.ToList();
            var allImageUrls = new List<string>();
            int deleted = 0, skipped = 0;

            foreach (var id in idList)
            {
                var (success, _, imageUrls) = await DeleteProductAsync(id);
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
                {
                    return await GetAllProductsAsync();
                }

                var lowerSearchTerm = searchTerm.ToLower();
                return await _context.Products
                    .Where(p => p.Name.ToLower().Contains(lowerSearchTerm) ||
                                p.Description.ToLower().Contains(lowerSearchTerm))
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
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
                var query = _context.Products.AsQueryable();

                // Search term filter
                if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
                {
                    var lowerSearchTerm = criteria.SearchTerm.ToLower();
                    query = query.Where(p => p.Name.ToLower().Contains(lowerSearchTerm) ||
                                            p.Description.ToLower().Contains(lowerSearchTerm));
                }

                // Category filter
                if (criteria.CategoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == criteria.CategoryId.Value);
                }

                // Brand filter
                if (criteria.BrandId.HasValue)
                {
                    query = query.Where(p => p.BrandId == criteria.BrandId.Value);
                }

                // Price range filter
                if (criteria.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= criteria.MinPrice.Value);
                }

                if (criteria.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= criteria.MaxPrice.Value);
                }

                // In stock filter
                if (criteria.InStock.HasValue && criteria.InStock.Value)
                {
                    query = query.Where(p => p.Inventories.Any(k => k.Quantity > 0));
                }

                // Active filter
                if (criteria.IsActive.HasValue && criteria.IsActive.Value)
                {
                    query = query.Where(p => p.Status == 1); // 1 = active
                }

                return await query
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Inventories)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering products");
                return new List<Product>();
            }
        }

        #endregion

        #region Check Existence

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

        public async Task<bool> CheckSkuExistsAsync(string sku, int? excludeProductId = null)
        {
            try
            {
                // Note: Product model doesn't have a SKU property. This method is here for interface compliance.
                // If SKU is needed, it should be added to the Product model.
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking SKU existence: {SKU}", sku);
                return false;
            }
        }

        #endregion
    }
}

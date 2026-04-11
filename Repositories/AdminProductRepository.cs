using HDKTech.Models;
using HDKTech.Data;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HDKTech.Repositories
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

        public async Task<IEnumerable<SanPham>> GetAllProductsAsync()
        {
            try
            {
                return await _context.SanPhams
                    .Include(p => p.DanhMuc)
                    .Include(p => p.HangSX)
                    .Include(p => p.HinhAnhs)
                    .OrderBy(p => p.TenSanPham)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                return new List<SanPham>();
            }
        }

        public async Task<SanPham> GetProductByIdAsync(int id)
        {
            try
            {
                return await _context.SanPhams
                    .Include(p => p.DanhMuc)
                    .Include(p => p.HangSX)
                    .Include(p => p.HinhAnhs)
                    .Include(p => p.KhoHangs)
                    .FirstOrDefaultAsync(p => p.MaSanPham == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by id: {ProductId}", id);
                return null;
            }
        }

        public async Task<IEnumerable<SanPham>> GetProductsByCategoryAsync(int categoryId)
        {
            try
            {
                return await _context.SanPhams
                    .Where(p => p.MaDanhMuc == categoryId)
                    .Include(p => p.DanhMuc)
                    .Include(p => p.HangSX)
                    .OrderBy(p => p.TenSanPham)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by category: {CategoryId}", categoryId);
                return new List<SanPham>();
            }
        }

        public async Task<IEnumerable<SanPham>> GetProductsByBrandAsync(int brandId)
        {
            try
            {
                return await _context.SanPhams
                    .Where(p => p.MaHangSX == brandId)
                    .Include(p => p.DanhMuc)
                    .Include(p => p.HangSX)
                    .OrderBy(p => p.TenSanPham)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by brand: {BrandId}", brandId);
                return new List<SanPham>();
            }
        }

        public async Task<(IEnumerable<SanPham> products, int totalCount)> GetProductsPagedAsync(int pageNumber, int pageSize)
        {
            try
            {
                var query = _context.SanPhams
                    .Include(p => p.DanhMuc)
                    .Include(p => p.HangSX)
                    .Include(p => p.HinhAnhs);

                var totalCount = await query.CountAsync();
                var products = await query
                    .OrderBy(p => p.TenSanPham)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (products, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged products");
                return (new List<SanPham>(), 0);
            }
        }

        #endregion

        #region Create Operations

        public async Task<SanPham> CreateProductAsync(SanPham product)
        {
            try
            {
                if (product == null)
                {
                    throw new ArgumentNullException(nameof(product));
                }

                _context.SanPhams.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product created successfully: {ProductName}", product.TenSanPham);
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

        public async Task<bool> UpdateProductAsync(SanPham product)
        {
            try
            {
                if (product == null)
                {
                    throw new ArgumentNullException(nameof(product));
                }

                var existingProduct = await _context.SanPhams.FindAsync(product.MaSanPham);
                if (existingProduct == null)
                {
                    _logger.LogWarning("Product not found for update: {ProductId}", product.MaSanPham);
                    return false;
                }

                // Update properties
                existingProduct.TenSanPham = product.TenSanPham;
                existingProduct.MoTaSanPham = product.MoTaSanPham;
                existingProduct.Gia = product.Gia;
                existingProduct.MaDanhMuc = product.MaDanhMuc;
                existingProduct.MaHangSX = product.MaHangSX;
                existingProduct.TrangThaiSanPham = product.TrangThaiSanPham;
                existingProduct.ThongSoKyThuat = product.ThongSoKyThuat;
                existingProduct.ThongTinBaoHanh = product.ThongTinBaoHanh;
                existingProduct.KhuyenMai = product.KhuyenMai;
                existingProduct.GiaNiemYet = product.GiaNiemYet;

                _context.SanPhams.Update(existingProduct);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product updated successfully: {ProductName}", product.TenSanPham);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", product.MaSanPham);
                return false;
            }
        }

        public async Task<bool> UpdateProductStockAsync(int productId, int quantity)
        {
            try
            {
                var product = await _context.SanPhams.FindAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for stock update: {ProductId}", productId);
                    return false;
                }

                // Update stock in KhoHang table (inventory is managed separately)
                var khoHang = await _context.KhoHangs.FirstOrDefaultAsync(k => k.MaSanPham == productId);
                if (khoHang != null)
                {
                    khoHang.SoLuong = quantity;
                    khoHang.NgayCapNhat = DateTime.Now;
                    _context.KhoHangs.Update(khoHang);
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
                var product = await _context.SanPhams.FindAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for price update: {ProductId}", productId);
                    return false;
                }

                product.Gia = price;
                _context.SanPhams.Update(product);
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

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.SanPhams.FindAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for deletion: {ProductId}", id);
                    return false;
                }

                _context.SanPhams.Remove(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product deleted successfully: {ProductId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteProductsAsync(IEnumerable<int> ids)
        {
            try
            {
                var products = await _context.SanPhams
                    .Where(p => ids.Contains(p.MaSanPham))
                    .ToListAsync();

                if (products.Count == 0)
                {
                    _logger.LogWarning("No products found for bulk deletion");
                    return false;
                }

                _context.SanPhams.RemoveRange(products);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Products deleted successfully: Count: {Count}", products.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting multiple products");
                return false;
            }
        }

        #endregion

        #region Search and Filter

        public async Task<IEnumerable<SanPham>> SearchProductsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllProductsAsync();
                }

                var lowerSearchTerm = searchTerm.ToLower();
                return await _context.SanPhams
                    .Where(p => p.TenSanPham.ToLower().Contains(lowerSearchTerm) ||
                                p.MoTaSanPham.ToLower().Contains(lowerSearchTerm))
                    .Include(p => p.DanhMuc)
                    .Include(p => p.HangSX)
                    .OrderBy(p => p.TenSanPham)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products: {SearchTerm}", searchTerm);
                return new List<SanPham>();
            }
        }

        public async Task<IEnumerable<SanPham>> FilterProductsAsync(ProductFilterCriteria criteria)
        {
            try
            {
                var query = _context.SanPhams.AsQueryable();

                // Search term filter
                if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
                {
                    var lowerSearchTerm = criteria.SearchTerm.ToLower();
                    query = query.Where(p => p.TenSanPham.ToLower().Contains(lowerSearchTerm) ||
                                            p.MoTaSanPham.ToLower().Contains(lowerSearchTerm));
                }

                // Category filter
                if (criteria.CategoryId.HasValue)
                {
                    query = query.Where(p => p.MaDanhMuc == criteria.CategoryId.Value);
                }

                // Brand filter
                if (criteria.BrandId.HasValue)
                {
                    query = query.Where(p => p.MaHangSX == criteria.BrandId.Value);
                }

                // Price range filter
                if (criteria.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Gia >= criteria.MinPrice.Value);
                }

                if (criteria.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Gia <= criteria.MaxPrice.Value);
                }

                // In stock filter
                if (criteria.InStock.HasValue && criteria.InStock.Value)
                {
                    query = query.Where(p => p.KhoHangs.Any(k => k.SoLuong > 0));
                }

                // Active filter
                if (criteria.IsActive.HasValue && criteria.IsActive.Value)
                {
                    query = query.Where(p => p.TrangThaiSanPham == 1); // Assuming 1 = active
                }

                return await query
                    .Include(p => p.DanhMuc)
                    .Include(p => p.HangSX)
                    .Include(p => p.HinhAnhs)
                    .Include(p => p.KhoHangs)
                    .OrderBy(p => p.TenSanPham)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering products");
                return new List<SanPham>();
            }
        }

        #endregion

        #region Check Existence

        public async Task<bool> ProductExistsAsync(int id)
        {
            try
            {
                return await _context.SanPhams.AnyAsync(p => p.MaSanPham == id);
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
                // Note: SanPham model doesn't have SKU property. This method is here for interface compliance.
                // If SKU is needed, it should be added to the SanPham model.
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

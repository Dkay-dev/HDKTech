using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    /// <summary>
    /// Interface định nghĩa các phép toán CRUD cho danh mục sản phẩm.
    /// </summary>
    public interface ICategoryRepository
    {
        /// <summary>Lấy toàn bộ danh mục, bao gồm số sản phẩm và danh mục con.</summary>
        Task<List<Category>> GetAllAsync();

        /// <summary>Lấy toàn bộ danh mục kèm thông tin Products và SubCategories.</summary>
        Task<List<Category>> GetAllWithDetailsAsync();

        /// <summary>Lấy các danh mục gốc (không có cha), dùng cho dropdown.</summary>
        Task<List<Category>> GetParentCategoriesAsync(int? excludeId = null);

        /// <summary>Lấy danh mục theo Id.</summary>
        Task<Category?> GetByIdAsync(int id);

        /// <summary>Lấy danh mục theo Id kèm Products và SubCategories.</summary>
        Task<Category?> GetByIdWithDetailsAsync(int id);

        /// <summary>Thêm mới danh mục.</summary>
        Task<bool> AddAsync(Category category);

        /// <summary>Cập nhật thông tin danh mục.</summary>
        Task<bool> UpdateAsync(Category category);

        /// <summary>Xóa danh mục theo Id.</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Kiểm tra danh mục có sản phẩm liên kết không.</summary>
        Task<bool> HasProductsAsync(int categoryId);

        /// <summary>Kiểm tra danh mục có danh mục con không.</summary>
        Task<bool> HasSubCategoriesAsync(int categoryId);

        /// <summary>Đếm tổng số danh mục.</summary>
        Task<int> CountAsync();

        /// <summary>Đếm số danh mục không có sản phẩm.</summary>
        Task<int> CountEmptyAsync();
    }
}

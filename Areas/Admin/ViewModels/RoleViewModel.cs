using HDKTech.Models;

namespace HDKTech.Areas.Admin.ViewModels
{
    /// <summary>
    /// ViewModel cho trang Phân Quyền (Role Index)
    /// </summary>
    public class RoleIndexViewModel
    {
        public List<Role> Roles { get; set; } = new();
        public int TotalRoles { get; set; }
        public int ActiveRoles { get; set; }
        public int TotalPermissions { get; set; }

        // Phân trang
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;

        // Filter
        public string? SearchTerm { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}

namespace HDKTech.Areas.Admin.Constants
{
    /// <summary>
    /// Tập trung toàn bộ hằng số dùng trong phân vùng Admin.
    /// Lợi ích: thay đổi 1 chỗ, áp dụng toàn bộ hệ thống.
    /// </summary>
    public static class AdminConstants
    {
        // ─── Roles ──────────────────────────────────────────────────────────
        public const string AdminRole     = "Admin";
        public const string ManagerRole   = "Manager";
        public const string AdminOrManager = "Admin,Manager";

        // ─── Pagination ─────────────────────────────────────────────────────
        public const int DefaultPageSize   = 10;
        public const int CategoryPageSize  = 20;
        public const int OrderPageSize     = 20;
        public const int MaxExportRows     = 10_000;

        // ─── Order status codes ──────────────────────────────────────────────
        public const int OrderPending    = 0; // Chờ xác nhận
        public const int OrderProcessing = 1; // Đang xử lý
        public const int OrderShipping   = 2; // Đang giao
        public const int OrderDelivered  = 3; // Đã giao
        public const int OrderCancelled  = 4; // Đã hủy

        // ─── Product / Inventory ─────────────────────────────────────────────
        public const int LowStockThreshold = 10;
        public const int ActiveProduct     = 1;
        public const int InactiveProduct   = 0;

        // ─── SystemLog action types ──────────────────────────────────────────
        public const string ActionCreate  = "Create";
        public const string ActionUpdate  = "Update";
        public const string ActionDelete  = "Delete";
        public const string ActionLogin   = "Login";
        public const string ActionLogout  = "Logout";
        public const string ActionExport  = "Export";

        // ─── Modules ─────────────────────────────────────────────────────────
        public const string ModuleDashboard  = "Dashboard";
        public const string ModuleProduct    = "Product";
        public const string ModuleCategory   = "Category";
        public const string ModuleBrand      = "Brand";
        public const string ModuleOrder      = "Order";
        public const string ModuleBanner     = "Banner";
        public const string ModulePromotion   = "Promotion";
        public const string ModuleRole       = "Role";
        public const string ModuleSystemLog  = "SystemLog";

        // ─── Banner types ─────────────────────────────────────────────────────
        public const string BannerTypeMain   = "Main";
        public const string BannerTypeSide   = "Side";
        public const string BannerTypeBottom = "Bottom";

        // ─── Upload paths (relative to wwwroot) ─────────────────────────────
        public const string BannerUploadFolder  = "uploads/banners";
        public const string ProductImageFolder  = "images/products";

        // ─── Hardcoded guest CategoryController IDs → sẽ bị xóa khi Guest team refactor
        public const int GuestCategoryBrandParentId = 15;
        public const int GuestCategoryPriceParentId = 21;
        public const int GuestCategoryCpuParentId   = 25;
        public const int GuestCategoryVgaParentId   = 26;
        public const int GuestCategoryRamParentId   = 27;
    }
}

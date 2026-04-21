using HDKTech.Models;

namespace HDKTech.Areas.Admin.ViewModels
{
    /// <summary>
    /// ViewModel cho trang Audit Log (SystemLog) - bảng Thời gian | Người dùng | Hành động | Chi tiết
    /// </summary>
    public class SystemLogViewModel
    {
        // Danh sách logs phân trang
        public List<SystemLog> Logs { get; set; } = new();

        // Thống kê tổng quan
        public int TotalLogs { get; set; }
        public int TodayActions { get; set; }
        public int LoginCount { get; set; }
        public int CreateCount { get; set; }
        public int UpdateCount { get; set; }
        public int DeleteCount { get; set; }

        // Phân trang
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }

        // Filter options
        public string? SearchText { get; set; }
        public string? SelectedActionType { get; set; }
        public string? SelectedModule { get; set; }
        public string? SelectedUsername { get; set; }

        // Dropdown options
        public List<string> ActionTypes { get; set; } = new();
        public List<string> Modules { get; set; } = new();
        public List<string> Usernames { get; set; } = new();

        // Helper: có trang trước/sau không
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}

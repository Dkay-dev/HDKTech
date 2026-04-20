namespace HDKTech.Services
{
    /// <summary>
    /// Giai đoạn 3 — Smart Reporting
    /// Định nghĩa các hàm xuất báo cáo Excel cho Admin.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Xuất báo cáo Doanh Thu theo khoảng thời gian.
        /// Chỉ lấy các đơn hàng có Status == 3 (Đã giao / Success).
        /// Các cột: Mã đơn, Khách hàng, Ngày thanh toán, Tổng tiền, Giảm giá.
        /// </summary>
        Task<byte[]> ExportRevenueExcelAsync(DateTime start, DateTime end);

        /// <summary>
        /// Xuất báo cáo Tồn Kho toàn bộ sản phẩm.
        /// Các cột: Tên sản phẩm, Danh mục, Giá nhập (ListPrice), Giá bán (Price), Số lượng tồn kho.
        /// Các dòng có tồn kho &lt; 5 được tô màu đỏ nhạt.
        /// </summary>
        Task<byte[]> ExportInventoryExcelAsync();
    }
}

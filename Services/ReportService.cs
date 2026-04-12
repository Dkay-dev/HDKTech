using ClosedXML.Excel;
using HDKTech.Data;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Services
{
    /// <summary>
    /// Giai đoạn 3 — Smart Reporting
    /// Triển khai xuất báo cáo Excel dùng ClosedXML.
    /// MemoryStream được dispose đúng cách sau khi lấy byte array.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly HDKTechContext _context;

        public ReportService(HDKTechContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Báo cáo Doanh Thu
        // ─────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<byte[]> ExportRevenueExcelAsync(DateTime start, DateTime end)
        {
            // Chuẩn hoá: start = đầu ngày, end = cuối ngày
            var startDate = start.Date;
            var endDate   = end.Date.AddDays(1).AddTicks(-1);

            // Chỉ lấy đơn Đã giao (Status == 3 = "Success")
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Items)
                .Where(o => o.Status == 3
                         && o.OrderDate >= startDate
                         && o.OrderDate <= endDate)
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            using var workbook  = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Doanh Thu");

            // ── Tiêu đề trang ────────────────────────────────────────────────
            ws.Cell("A1").Value = $"BÁO CÁO DOANH THU  |  {startDate:dd/MM/yyyy} – {end.Date:dd/MM/yyyy}";
            var titleRange = ws.Range("A1:E1");
            titleRange.Merge();
            titleRange.Style.Font.Bold      = true;
            titleRange.Style.Font.FontSize  = 13;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // ── Header hàng 2 ────────────────────────────────────────────────
            string[] headers = { "Mã đơn hàng", "Khách hàng", "Ngày thanh toán", "Tổng tiền (VNĐ)", "Giảm giá (VNĐ)" };
            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = ws.Cell(2, col);
                cell.Value = headers[col - 1];
                cell.Style.Font.Bold      = true;
                cell.Style.Fill.BackgroundColor  = XLColor.FromHtml("#4472C4");
                cell.Style.Font.FontColor        = XLColor.White;
                cell.Style.Alignment.Horizontal  = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder  = XLBorderStyleValues.Thin;
            }

            // ── Dữ liệu ──────────────────────────────────────────────────────
            int row = 3;
            foreach (var order in orders)
            {
                // Giảm giá = Tổng item - (TotalAmount - ShippingFee) nếu dương
                decimal itemsSubtotal = order.Items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0m;
                decimal paid          = order.TotalAmount - order.ShippingFee;
                decimal discount      = Math.Max(0m, itemsSubtotal - paid);

                ws.Cell(row, 1).Value = order.OrderCode;
                ws.Cell(row, 2).Value = order.RecipientName
                                        ?? order.User?.UserName
                                        ?? "—";
                ws.Cell(row, 3).Value = order.OrderDate;
                ws.Cell(row, 3).Style.NumberFormat.Format = "dd/MM/yyyy HH:mm";
                ws.Cell(row, 4).Value = order.TotalAmount;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 5).Value = discount;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

                // Zebra stripe
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, 5)
                      .Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                }

                row++;
            }

            // ── Tổng cộng ────────────────────────────────────────────────────
            if (orders.Any())
            {
                ws.Cell(row, 3).Value = "Tổng cộng:";
                ws.Cell(row, 3).Style.Font.Bold      = true;
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(row, 4).FormulaA1 = $"=SUM(D3:D{row - 1})";
                ws.Cell(row, 4).Style.Font.Bold      = true;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";

                ws.Cell(row, 5).FormulaA1 = $"=SUM(E3:E{row - 1})";
                ws.Cell(row, 5).Style.Font.Bold      = true;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            }

            // ── Tự giãn cột ──────────────────────────────────────────────────
            ws.Columns().AdjustToContents();

            // ── Xuất về byte array ────────────────────────────────────────────
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Báo cáo Tồn Kho
        // ─────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<byte[]> ExportInventoryExcelAsync()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .OrderBy(p => p.Name)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Tồn Kho");

            // ── Tiêu đề trang ────────────────────────────────────────────────
            ws.Cell("A1").Value = $"BÁO CÁO TỒN KHO  |  Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            var titleRange = ws.Range("A1:E1");
            titleRange.Merge();
            titleRange.Style.Font.Bold     = true;
            titleRange.Style.Font.FontSize = 13;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // ── Header hàng 2 ────────────────────────────────────────────────
            string[] headers = { "Tên sản phẩm", "Danh mục", "Giá nhập (VNĐ)", "Giá bán (VNĐ)", "Tồn kho" };
            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = ws.Cell(2, col);
                cell.Value = headers[col - 1];
                cell.Style.Font.Bold     = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#375623");
                cell.Style.Font.FontColor       = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // ── Dữ liệu ──────────────────────────────────────────────────────
            int row = 3;
            foreach (var product in products)
            {
                int qty = product.Inventories?.Sum(i => i.Quantity) ?? 0;

                ws.Cell(row, 1).Value = product.Name;
                ws.Cell(row, 2).Value = product.Category?.Name ?? "—";
                ws.Cell(row, 3).Value = product.ListPrice ?? product.Price;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 4).Value = product.Price;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 5).Value = qty;
                ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Tô màu đỏ nhạt cho dòng có tồn kho < 5
                if (qty < 5)
                {
                    var dangerRange = ws.Range(row, 1, row, 5);
                    dangerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFCCCC");
                    dangerRange.Style.Font.FontColor       = XLColor.FromHtml("#CC0000");
                    ws.Cell(row, 5).Style.Font.Bold        = true;
                }
                else if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, 5)
                      .Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF1DE");
                }

                row++;
            }

            // ── Chú thích màu sắc ─────────────────────────────────────────────
            ws.Cell(row + 1, 1).Value = "* Dòng màu đỏ: tồn kho dưới 5 đơn vị — cần nhập thêm hàng.";
            ws.Cell(row + 1, 1).Style.Font.Italic    = true;
            ws.Cell(row + 1, 1).Style.Font.FontColor = XLColor.FromHtml("#CC0000");

            // ── Tự giãn cột ──────────────────────────────────────────────────
            ws.Columns().AdjustToContents();

            // ── Xuất về byte array ────────────────────────────────────────────
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}

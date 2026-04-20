using ClosedXML.Excel;
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Services
{
    /// <summary>
    /// Báo cáo Excel — refactor:
    ///  - Doanh thu dựa trên OrderItem snapshot (UnitPrice / LineTotal).
    ///  - Báo cáo tồn kho in theo ProductVariant (Sku, Price, ListPrice, Quantity).
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly HDKTechContext _context;

        public ReportService(HDKTechContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────
        // Doanh thu
        // ─────────────────────────────────────────────────────────────
        public async Task<byte[]> ExportRevenueExcelAsync(DateTime start, DateTime end)
        {
            var startDate = start.Date;
            var endDate   = end.Date.AddDays(1).AddTicks(-1);

            // Order.Status == Delivered (enum giá trị 4)
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Items)
                .Where(o => o.Status == OrderStatus.Delivered
                         && o.OrderDate >= startDate
                         && o.OrderDate <= endDate)
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Doanh Thu");

            ws.Cell("A1").Value = $"BÁO CÁO DOANH THU  |  {startDate:dd/MM/yyyy} – {end.Date:dd/MM/yyyy}";
            var titleRange = ws.Range("A1:E1");
            titleRange.Merge();
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 13;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] headers = { "Mã đơn hàng", "Khách hàng", "Ngày thanh toán", "Tổng tiền (VNĐ)", "Giảm giá (VNĐ)" };
            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = ws.Cell(2, col);
                cell.Value = headers[col - 1];
                cell.Style.Font.Bold            = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                cell.Style.Font.FontColor       = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int row = 3;
            foreach (var order in orders)
            {
                decimal itemsSubtotal = order.Items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0m;
                decimal paid          = order.TotalAmount - order.ShippingFee;
                decimal discount      = Math.Max(0m, itemsSubtotal - paid);

                ws.Cell(row, 1).Value = order.OrderCode;
                ws.Cell(row, 2).Value = order.RecipientName ?? order.User?.UserName ?? "—";
                ws.Cell(row, 3).Value = order.OrderDate;
                ws.Cell(row, 3).Style.NumberFormat.Format = "dd/MM/yyyy HH:mm";
                ws.Cell(row, 4).Value = order.TotalAmount;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 5).Value = discount;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

                if (row % 2 == 0)
                    ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");

                row++;
            }

            if (orders.Any())
            {
                ws.Cell(row, 3).Value = "Tổng cộng:";
                ws.Cell(row, 3).Style.Font.Bold = true;
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(row, 4).FormulaA1 = $"=SUM(D3:D{row - 1})";
                ws.Cell(row, 4).Style.Font.Bold = true;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";

                ws.Cell(row, 5).FormulaA1 = $"=SUM(E3:E{row - 1})";
                ws.Cell(row, 5).Style.Font.Bold = true;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        // ─────────────────────────────────────────────────────────────
        // Tồn kho — in theo Variant (SKU là đơn vị kho thực sự)
        // ─────────────────────────────────────────────────────────────
        public async Task<byte[]> ExportInventoryExcelAsync()
        {
            var variants = await _context.ProductVariants
                .AsNoTracking()
                .Include(v => v.Product).ThenInclude(p => p!.Category)
                .Include(v => v.Inventories)
                .OrderBy(v => v.Product!.Name)
                .ThenBy(v => v.Sku)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Tồn Kho");

            ws.Cell("A1").Value = $"BÁO CÁO TỒN KHO  |  Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            var titleRange = ws.Range("A1:F1");
            titleRange.Merge();
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 13;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] headers = { "Tên sản phẩm", "SKU / Cấu hình", "Danh mục", "Giá niêm yết (VNĐ)", "Giá bán (VNĐ)", "Tồn kho" };
            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = ws.Cell(2, col);
                cell.Value = headers[col - 1];
                cell.Style.Font.Bold            = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#375623");
                cell.Style.Font.FontColor       = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int row = 3;
            foreach (var v in variants)
            {
                int qty = v.Inventories?.Sum(i => i.Quantity) ?? 0;

                ws.Cell(row, 1).Value = v.Product?.Name ?? "—";
                ws.Cell(row, 2).Value = string.IsNullOrWhiteSpace(v.VariantName) ? v.Sku : $"{v.Sku} — {v.VariantName}";
                ws.Cell(row, 3).Value = v.Product?.Category?.Name ?? "—";
                ws.Cell(row, 4).Value = v.ListPrice ?? v.Price;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 5).Value = v.Price;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 6).Value = qty;
                ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (qty < 5)
                {
                    var dangerRange = ws.Range(row, 1, row, 6);
                    dangerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFCCCC");
                    dangerRange.Style.Font.FontColor       = XLColor.FromHtml("#CC0000");
                    ws.Cell(row, 6).Style.Font.Bold        = true;
                }
                else if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF1DE");
                }

                row++;
            }

            ws.Cell(row + 1, 1).Value = "* Dòng màu đỏ: tồn kho dưới 5 đơn vị — cần nhập thêm hàng.";
            ws.Cell(row + 1, 1).Style.Font.Italic    = true;
            ws.Cell(row + 1, 1).Style.Font.FontColor = XLColor.FromHtml("#CC0000");

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}

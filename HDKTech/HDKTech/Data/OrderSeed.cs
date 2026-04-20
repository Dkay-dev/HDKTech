using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    /// <summary>
    /// Seed Orders + OrderItems theo schema mới.
    ///
    /// Migration so với bản cũ:
    ///  - ShippingAddress (string duy nhất) ─▶ ShippingAddressLine/Ward/District/City
    ///    + ShippingAddressFull (composed) — bóc tách từ chuỗi cũ.
    ///  - Status (int) ─▶ enum OrderStatus.
    ///  - Thêm SubTotal / DiscountAmount / TotalAmount tách bạch
    ///    (TotalAmount = SubTotal - DiscountAmount + ShippingFee).
    ///  - OrderItem: thêm ProductVariantId (default variant = <see cref="SeedConstants.DefaultVariantId"/>)
    ///    + snapshot ProductNameSnapshot / SkuSnapshot / SpecSnapshot / LineTotal.
    ///  - PaymentMethod/PaymentStatus/PaidAt cho các đơn đã giao.
    ///
    /// Phải chạy SAU ProductVariantSeed.
    /// </summary>
    public static class OrderSeed
    {
        /// <summary>Mô tả 1 đơn hàng cho seed (tuple thay cho phép record).</summary>
        private record OrderRow(
            string UserId, string Code, string Name, string Phone, string Addr,
            OrderStatus Status, int DaysAgo, int ProductId, int Qty, decimal UnitPrice);

        public static async Task SeedAsync(HDKTechContext context)
        {
            if (await context.Orders.AnyAsync()) return;

            // Tra cứu variant default của mỗi Product (1 query, in-memory lookup)
            var variantLookup = await context.ProductVariants
                .Where(v => v.IsDefault)
                .Select(v => new { v.Id, v.ProductId, v.Sku })
                .ToDictionaryAsync(v => v.ProductId, v => (v.Id, v.Sku));

            // Tra cứu name/specs từ ProductSeed.Rows (single source of truth)
            var productLookup = ProductSeed.Rows.ToDictionary(
                r => r.Id, r => (Name: r.Name, Specs: r.Specs));

            var orderData = new OrderRow[]
            {
                // Hôm nay
                new(SeedConstants.User1Id, "HDK20260417001",
                    "Nguyễn Văn An", "0905123456",
                    "123 Hoàng Diệu, Phước Ninh, Hải Châu, Đà Nẵng",
                    OrderStatus.Shipping, 0, 11, 1, 54_990_000m),

                new(SeedConstants.User2Id, "HDK20260417002",
                    "Trần Thị Bích", "0936789012",
                    "45 Lê Lợi, Thạch Thang, Thanh Khê, Đà Nẵng",
                    OrderStatus.Confirmed, 0, 37, 2, 2_990_000m),

                // Hôm qua
                new(SeedConstants.User3Id, "HDK20260416001",
                    "Lê Quốc Hùng", "0914567890",
                    "78 Nguyễn Tri Phương, Hòa Khánh Bắc, Liên Chiểu, Đà Nẵng",
                    OrderStatus.Shipping, 1, 4, 1, 32_990_000m),

                new(SeedConstants.User4Id, "HDK20260416002",
                    "Phạm Minh Đức", "0977234567",
                    "12 Tôn Đức Thắng, Hòa Hiệp Nam, Liên Chiểu, Đà Nẵng",
                    OrderStatus.Packing, 1, 29, 1, 92_990_000m),

                // 2 ngày trước
                new(SeedConstants.User5Id, "HDK20260415001",
                    "Hoàng Thị Lan", "0967890123",
                    "56 Điện Biên Phủ, Chính Gián, Thanh Khê, Đà Nẵng",
                    OrderStatus.Shipping, 2, 61, 2, 9_490_000m),

                new(SeedConstants.User1Id, "HDK20260415002",
                    "Nguyễn Văn An", "0905123456",
                    "123 Hoàng Diệu, Phước Ninh, Hải Châu, Đà Nẵng",
                    OrderStatus.Delivered, 2, 39, 1, 2_790_000m),

                // 3 ngày trước
                new(SeedConstants.User2Id, "HDK20260414001",
                    "Trần Thị Bích", "0936789012",
                    "45 Lê Lợi, Thạch Thang, Thanh Khê, Đà Nẵng",
                    OrderStatus.Shipping, 3, 13, 1, 45_990_000m),

                new(SeedConstants.User3Id, "HDK20260414002",
                    "Lê Quốc Hùng", "0914567890",
                    "78 Nguyễn Tri Phương, Hòa Khánh Bắc, Liên Chiểu, Đà Nẵng",
                    OrderStatus.Pending, 3, 32, 2, 4_490_000m),

                // 5 ngày trước
                new(SeedConstants.User4Id, "HDK20260412001",
                    "Phạm Minh Đức", "0977234567",
                    "12 Tôn Đức Thắng, Hòa Hiệp Nam, Liên Chiểu, Đà Nẵng",
                    OrderStatus.Shipping, 5, 22, 1, 68_990_000m),

                new(SeedConstants.User5Id, "HDK20260412002",
                    "Hoàng Thị Lan", "0967890123",
                    "56 Điện Biên Phủ, Chính Gián, Thanh Khê, Đà Nẵng",
                    OrderStatus.Shipping, 5, 41, 1, 2_790_000m),

                // 7 ngày trước
                new(SeedConstants.User1Id, "HDK20260410001",
                    "Nguyễn Văn An", "0905123456",
                    "123 Hoàng Diệu, Phước Ninh, Hải Châu, Đà Nẵng",
                    OrderStatus.Shipping, 7, 30, 1, 49_990_000m),

                new(SeedConstants.User2Id, "HDK20260410002",
                    "Trần Thị Bích", "0936789012",
                    "45 Lê Lợi, Thạch Thang, Thanh Khê, Đà Nẵng",
                    OrderStatus.Confirmed, 7, 64, 1, 14_490_000m),

                // 10 ngày trước
                new(SeedConstants.User3Id, "HDK20260407001",
                    "Lê Quốc Hùng", "0914567890",
                    "78 Nguyễn Tri Phương, Hòa Khánh Bắc, Liên Chiểu, Đà Nẵng",
                    OrderStatus.Delivered, 10, 6, 1, 11_990_000m),

                new(SeedConstants.User4Id, "HDK20260407002",
                    "Phạm Minh Đức", "0977234567",
                    "12 Tôn Đức Thắng, Hòa Hiệp Nam, Liên Chiểu, Đà Nẵng",
                    OrderStatus.Delivered, 10, 14, 1, 62_990_000m),

                // 14 ngày trước
                new(SeedConstants.User5Id, "HDK20260403001",
                    "Hoàng Thị Lan", "0967890123",
                    "56 Điện Biên Phủ, Chính Gián, Thanh Khê, Đà Nẵng",
                    OrderStatus.Delivered, 14, 38, 1, 2_490_000m),

                new(SeedConstants.User1Id, "HDK20260403002",
                    "Nguyễn Văn An", "0905123456",
                    "123 Hoàng Diệu, Phước Ninh, Hải Châu, Đà Nẵng",
                    OrderStatus.Delivered, 14, 31, 2, 4_990_000m),

                // 20 ngày trước
                new(SeedConstants.User2Id, "HDK20260328001",
                    "Trần Thị Bích", "0936789012",
                    "45 Lê Lợi, Thạch Thang, Thanh Khê, Đà Nẵng",
                    OrderStatus.Delivered, 20, 1, 1, 26_990_000m),

                new(SeedConstants.User3Id, "HDK20260328002",
                    "Lê Quốc Hùng", "0914567890",
                    "78 Nguyễn Tri Phương, Hòa Khánh Bắc, Liên Chiểu, Đà Nẵng",
                    OrderStatus.Delivered, 20, 40, 1, 5_490_000m),

                // 30 ngày trước
                new(SeedConstants.User4Id, "HDK20260318001",
                    "Phạm Minh Đức", "0977234567",
                    "12 Tôn Đức Thắng, Hòa Hiệp Nam, Liên Chiểu, Đà Nẵng",
                    OrderStatus.Delivered, 30, 23, 1, 155_000_000m),
            };

            using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Orders] ON");

                var today = DateTime.Now;
                int orderId = 1;

                foreach (var o in orderData)
                {
                    if (!variantLookup.TryGetValue(o.ProductId, out var variant))
                        throw new InvalidOperationException(
                            $"OrderSeed: không tìm thấy ProductVariant cho ProductId={o.ProductId}. " +
                            "Đảm bảo ProductVariantSeed đã chạy trước.");

                    var product = productLookup.TryGetValue(o.ProductId, out var p) ? p : default;

                    // Giá đã có discount sẵn từ promotion → DiscountAmount = 0 cho đơn giản seed
                    var subTotal       = o.Qty * o.UnitPrice;
                    var discountAmount = 0m;
                    var shippingFee    = subTotal > 5_000_000m ? 0m : 30_000m;
                    var totalAmount    = subTotal - discountAmount + shippingFee;

                    // Tách address: "line, [ward,] district, city"
                    var parts = o.Addr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    string line = parts.Length > 0 ? parts[0] : o.Addr;
                    string ward = parts.Length >= 4 ? parts[1] : string.Empty;
                    string district = parts.Length >= 4 ? parts[2]
                                    : parts.Length >= 3 ? parts[1] : string.Empty;
                    string city = parts.Length >= 2 ? parts[^1] : "Đà Nẵng";

                    // Timestamp ngẫu nhiên trong khung giờ mở cửa
                    var rng = new Random(orderId * 31);
                    var orderDate = today.Date.AddDays(-o.DaysAgo)
                                              .AddHours(rng.Next(8, 20))
                                              .AddMinutes(rng.Next(0, 60));

                    // Payment: cash-on-delivery, nhưng các đơn Delivered đã được thu tiền
                    var paymentStatus = o.Status == OrderStatus.Delivered
                        ? PaymentStatus.Paid : PaymentStatus.Unpaid;
                    DateTime? paidAt = paymentStatus == PaymentStatus.Paid
                        ? orderDate.AddDays(2) : null;

                    // Timeline theo trạng thái
                    DateTime? confirmedAt = o.Status >= OrderStatus.Confirmed ? orderDate.AddHours(2) : null;
                    DateTime? shippedAt   = o.Status >= OrderStatus.Shipping  ? orderDate.AddDays(1)  : null;
                    DateTime? deliveredAt = o.Status == OrderStatus.Delivered ? orderDate.AddDays(2)  : null;

                    var order = new Order
                    {
                        Id                  = orderId,
                        OrderCode           = o.Code,
                        UserId              = o.UserId,
                        UserAddressId       = null,   // địa chỉ có thể đã đổi; snapshot ở dưới
                        SubTotal            = subTotal,
                        DiscountAmount      = discountAmount,
                        ShippingFee         = shippingFee,
                        TotalAmount         = totalAmount,
                        RecipientName       = o.Name,
                        RecipientPhone      = o.Phone,
                        ShippingAddressLine = line,
                        ShippingWard        = ward,
                        ShippingDistrict    = district,
                        ShippingCity        = city,
                        ShippingAddressFull = o.Addr,
                        Status              = o.Status,
                        OrderDate           = orderDate,
                        ConfirmedAt         = confirmedAt,
                        ShippedAt           = shippedAt,
                        DeliveredAt         = deliveredAt,
                        PaymentMethod       = "COD",
                        PaymentStatus       = paymentStatus,
                        PaidAt              = paidAt,
                        Items = new List<OrderItem>
                        {
                            new OrderItem
                            {
                                OrderId             = orderId,
                                ProductId           = o.ProductId,
                                ProductVariantId    = variant.Id,
                                ProductNameSnapshot = product.Name ?? $"Product #{o.ProductId}",
                                SkuSnapshot         = variant.Sku,
                                SpecSnapshot        = Truncate(product.Specs, 500),
                                Quantity            = o.Qty,
                                UnitPrice           = o.UnitPrice,
                                DiscountAmount      = 0m,
                                LineTotal           = o.Qty * o.UnitPrice
                            }
                        }
                    };

                    context.Orders.Add(order);
                    orderId++;
                }

                await context.SaveChangesAsync();
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Orders] OFF");
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static string? Truncate(string? s, int max) =>
            string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
    }
}

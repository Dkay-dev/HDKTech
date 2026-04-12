using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data.Seeds
{
    public static class OrderSeed
    {
        public static async Task SeedAsync(HDKTechContext context)
        {
            if (await context.Orders.AnyAsync()) return;

            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Orders] ON");

                    var now = DateTime.Now;

            // Dữ liệu đơn hàng đa dạng: nhiều trạng thái, nhiều ngày khác nhau
            // để Dashboard doanh thu có biểu đồ đẹp
            var orderData = new[]
            {
                // Hôm nay
                (userId: SeedConstants.User1Id, code: "HDK20241201001",
                 name: "Nguyễn Văn An", phone: "0905123456",
                 addr: "123 Hoàng Diệu, Hải Châu, Đà Nẵng",
                 status: 3, daysAgo: 0, productId: 11, qty: 1, unitPrice: 54_990_000m),

                (userId: SeedConstants.User2Id, code: "HDK20241201002",
                 name: "Trần Thị Bích", phone: "0936789012",
                 addr: "45 Lê Lợi, Thanh Khê, Đà Nẵng",
                 status: 1, daysAgo: 0, productId: 37, qty: 2, unitPrice: 2_990_000m),

                // Hôm qua
                (userId: SeedConstants.User3Id, code: "HDK20241130001",
                 name: "Lê Quốc Hùng", phone: "0914567890",
                 addr: "78 Nguyễn Tri Phương, Hòa Khánh Bắc, Liên Chiểu, Đà Nẵng",
                 status: 3, daysAgo: 1, productId: 4, qty: 1, unitPrice: 32_990_000m),

                (userId: SeedConstants.User4Id, code: "HDK20241130002",
                 name: "Phạm Minh Đức", phone: "0977234567",
                 addr: "12 Tôn Đức Thắng, Hòa Hiệp Nam, Liên Chiểu, Đà Nẵng",
                 status: 2, daysAgo: 1, productId: 29, qty: 1, unitPrice: 92_990_000m),

                // 2 ngày trước
                (userId: SeedConstants.User5Id, code: "HDK20241129001",
                 name: "Hoàng Thị Lan", phone: "0967890123",
                 addr: "56 Điện Biên Phủ, Chính Gián, Thanh Khê, Đà Nẵng",
                 status: 3, daysAgo: 2, productId: 61, qty: 2, unitPrice: 9_490_000m),

                (userId: SeedConstants.User1Id, code: "HDK20241129002",
                 name: "Nguyễn Văn An", phone: "0905123456",
                 addr: "123 Hoàng Diệu, Hải Châu, Đà Nẵng",
                 status: 4, daysAgo: 2, productId: 39, qty: 1, unitPrice: 2_790_000m),

                // 3 ngày trước
                (userId: SeedConstants.User2Id, code: "HDK20241128001",
                 name: "Trần Thị Bích", phone: "0936789012",
                 addr: "45 Lê Lợi, Thanh Khê, Đà Nẵng",
                 status: 3, daysAgo: 3, productId: 13, qty: 1, unitPrice: 45_990_000m),

                (userId: SeedConstants.User3Id, code: "HDK20241128002",
                 name: "Lê Quốc Hùng", phone: "0914567890",
                 addr: "78 Nguyễn Tri Phương, Hòa Khánh Bắc, Liên Chiểu, Đà Nẵng",
                 status: 0, daysAgo: 3, productId: 32, qty: 2, unitPrice: 4_490_000m),

                // 5 ngày trước
                (userId: SeedConstants.User4Id, code: "HDK20241126001",
                 name: "Phạm Minh Đức", phone: "0977234567",
                 addr: "12 Tôn Đức Thắng, Hòa Hiệp Nam, Liên Chiểu, Đà Nẵng",
                 status: 3, daysAgo: 5, productId: 22, qty: 1, unitPrice: 68_990_000m),

                (userId: SeedConstants.User5Id, code: "HDK20241126002",
                 name: "Hoàng Thị Lan", phone: "0967890123",
                 addr: "56 Điện Biên Phủ, Chính Gián, Thanh Khê, Đà Nẵng",
                 status: 3, daysAgo: 5, productId: 41, qty: 1, unitPrice: 2_790_000m),

                // 7 ngày trước
                (userId: SeedConstants.User1Id, code: "HDK20241124001",
                 name: "Nguyễn Văn An", phone: "0905123456",
                 addr: "123 Hoàng Diệu, Hải Châu, Đà Nẵng",
                 status: 3, daysAgo: 7, productId: 30, qty: 1, unitPrice: 49_990_000m),

                (userId: SeedConstants.User2Id, code: "HDK20241124002",
                 name: "Trần Thị Bích", phone: "0936789012",
                 addr: "45 Lê Lợi, Thanh Khê, Đà Nẵng",
                 status: 1, daysAgo: 7, productId: 64, qty: 1, unitPrice: 14_490_000m),

                // 10 ngày trước
                (userId: SeedConstants.User3Id, code: "HDK20241121001",
                 name: "Lê Quốc Hùng", phone: "0914567890",
                 addr: "78 Nguyễn Tri Phương, Hòa Khánh Bắc, Liên Chiểu, Đà Nẵng",
                 status: 3, daysAgo: 10, productId: 6, qty: 1, unitPrice: 11_990_000m),

                (userId: SeedConstants.User4Id, code: "HDK20241121002",
                 name: "Phạm Minh Đức", phone: "0977234567",
                 addr: "12 Tôn Đức Thắng, Hòa Hiệp Nam, Liên Chiểu, Đà Nẵng",
                 status: 3, daysAgo: 10, productId: 14, qty: 1, unitPrice: 62_990_000m),

                // 14 ngày trước
                (userId: SeedConstants.User5Id, code: "HDK20241117001",
                 name: "Hoàng Thị Lan", phone: "0967890123",
                 addr: "56 Điện Biên Phủ, Chính Gián, Thanh Khê, Đà Nẵng",
                 status: 3, daysAgo: 14, productId: 38, qty: 1, unitPrice: 2_490_000m),

                (userId: SeedConstants.User1Id, code: "HDK20241117002",
                 name: "Nguyễn Văn An", phone: "0905123456",
                 addr: "123 Hoàng Diệu, Hải Châu, Đà Nẵng",
                 status: 3, daysAgo: 14, productId: 31, qty: 2, unitPrice: 4_990_000m),

                // 20 ngày trước
                (userId: SeedConstants.User2Id, code: "HDK20241111001",
                 name: "Trần Thị Bích", phone: "0936789012",
                 addr: "45 Lê Lợi, Thanh Khê, Đà Nẵng",
                 status: 3, daysAgo: 20, productId: 1, qty: 1, unitPrice: 26_990_000m),

                (userId: SeedConstants.User3Id, code: "HDK20241111002",
                 name: "Lê Quốc Hùng", phone: "0914567890",
                 addr: "78 Nguyễn Tri Phương, Hòa Khánh Bắc, Liên Chiểu, Đà Nẵng",
                 status: 4, daysAgo: 20, productId: 40, qty: 1, unitPrice: 5_490_000m),

                // 30 ngày trước
                (userId: SeedConstants.User4Id, code: "HDK20241101001",
                 name: "Phạm Minh Đức", phone: "0977234567",
                 addr: "12 Tôn Đức Thắng, Hòa Hiệp Nam, Liên Chiểu, Đà Nẵng",
                 status: 3, daysAgo: 30, productId: 23, qty: 1, unitPrice: 155_000_000m),
            };

            int orderId = 1;
            foreach (var o in orderData)
            {
                var totalAmount = o.qty * o.unitPrice;
                var shippingFee = totalAmount > 5_000_000 ? 0m : 30_000m;
                var orderDate = now.Date.AddDays(-o.daysAgo)
                                        .AddHours(new Random(orderId).Next(8, 20))
                                        .AddMinutes(new Random(orderId * 7).Next(0, 60));

                var order = new Order
                {
                    Id = orderId,
                    OrderCode = o.code,
                    UserId = o.userId,
                    RecipientName = o.name,
                    RecipientPhone = o.phone,
                    ShippingAddress = o.addr,
                    TotalAmount = totalAmount,
                    ShippingFee = shippingFee,
                    Status = o.status,
                    OrderDate = orderDate,
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            OrderId    = orderId,
                            ProductId  = o.productId,
                            Quantity   = o.qty,
                            UnitPrice  = o.unitPrice
                        }
                    }
                };

                context.Orders.Add(order);
                orderId++;
            }

            await context.SaveChangesAsync();

                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Orders] OFF");
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}
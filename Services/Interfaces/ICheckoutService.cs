using HDKTech.Models;
using HDKTech.Models.Vnpay;

namespace HDKTech.Services.Interfaces
{
    public interface ICheckoutService
    {
        // Re-fetch giá variants từ DB (server-side price validation)
        Task<Dictionary<int, ProductVariant>> GetVariantPricesAsync(List<int> variantIds);

        // Tạo PendingCheckout và lưu DB
        Task<PendingCheckout> CreatePendingCheckoutAsync(PendingCheckout pending);

        // Cập nhật trạng thái PendingCheckout
        Task UpdatePendingStatusAsync(Guid id, CheckoutStatus status);

        // Lấy PendingCheckout theo ID
        Task<PendingCheckout?> GetPendingCheckoutAsync(Guid id);

        // Lưu log VNPay
        Task SaveVnPayLogAsync(VNPAYModel log);

        // Kiểm tra idempotency theo gateway transaction
        Task<PaymentTransaction?> FindExistingTransactionAsync(string gatewayTransactionId, PaymentGateway gateway);

        // Lấy Order từ PaymentTransaction
        Task<Order?> GetOrderByTransactionAsync(int orderId);

        // Tạo Order từ PendingCheckout (delegates to OrderRepository)
        Task<(bool Success, string? Error, Order? Order)> CreateOrderFromPendingAsync(PendingCheckout pending);

        // Lưu PaymentTransaction
        Task SavePaymentTransactionAsync(PaymentTransaction tx);
    }
}

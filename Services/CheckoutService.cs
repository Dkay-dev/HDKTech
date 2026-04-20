using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Models.Vnpay;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly HDKTechContext   _context;
        private readonly IOrderRepository _orderRepository;

        public CheckoutService(HDKTechContext context, IOrderRepository orderRepository)
        {
            _context         = context;
            _orderRepository = orderRepository;
        }

        public async Task<Dictionary<int, ProductVariant>> GetVariantPricesAsync(List<int> variantIds)
            => await _context.ProductVariants.AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

        public async Task<PendingCheckout> CreatePendingCheckoutAsync(PendingCheckout pending)
        {
            _context.PendingCheckouts.Add(pending);
            await _context.SaveChangesAsync();
            return pending;
        }

        public async Task UpdatePendingStatusAsync(Guid id, CheckoutStatus status)
        {
            var pending = await _context.PendingCheckouts.FindAsync(id);
            if (pending != null)
            {
                pending.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        public Task<PendingCheckout?> GetPendingCheckoutAsync(Guid id)
            => _context.PendingCheckouts.FindAsync(id).AsTask();

        public async Task SaveVnPayLogAsync(VNPAYModel log)
        {
            _context.VNPAYModels.Add(log);
            await _context.SaveChangesAsync();
        }

        public Task<PaymentTransaction?> FindExistingTransactionAsync(
            string gatewayTransactionId, PaymentGateway gateway)
            => _context.PaymentTransactions
                .FirstOrDefaultAsync(t =>
                    t.GatewayTransactionId == gatewayTransactionId &&
                    t.Gateway == gateway);

        public async Task<Order?> GetOrderByTransactionAsync(int orderId)
            => await _context.Orders.FindAsync(orderId);

        public Task<(bool Success, string? Error, Order? Order)> CreateOrderFromPendingAsync(PendingCheckout pending)
            => _orderRepository.CreateFromPendingCheckoutAsync(pending);

        public async Task SavePaymentTransactionAsync(PaymentTransaction tx)
        {
            _context.PaymentTransactions.Add(tx);
            await _context.SaveChangesAsync();
        }
    }
}

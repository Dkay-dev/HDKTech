using HDKTech.Areas.Admin.Models;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Models.Vnpay;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        public async Task<Dictionary<int, decimal>> GetVariantEffectivePricesAsync(List<int> variantIds)
        {
            var now = DateTime.Now;

            var variants = await _context.ProductVariants.AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .Select(v => new { v.Id, v.Price, v.ProductId })
                .ToListAsync();

            var productIds = variants.Select(v => v.ProductId).Distinct().ToList();

            // Lấy tất cả flash sale deals liên quan đến các variant/product đang checkout
            var flashDeals = await _context.PromotionProducts
                .Where(pp => pp.Promotion != null
                          && pp.Promotion.PromotionType == PromotionType.FlashSale
                          && pp.Promotion.IsActive
                          && pp.Promotion.StartDate <= now
                          && pp.Promotion.EndDate   >= now
                          && !pp.IsExclusion
                          && (pp.ProductVariantId.HasValue && variantIds.Contains(pp.ProductVariantId.Value)
                           || pp.ProductId.HasValue        && productIds.Contains(pp.ProductId.Value)))
                .Select(pp => new
                {
                    pp.ProductVariantId,
                    pp.ProductId,
                    SalePrice = pp.Promotion!.Value
                })
                .ToListAsync();

            var result = new Dictionary<int, decimal>();
            foreach (var v in variants)
            {
                // Ưu tiên: variant-level deal → product-level deal → giá gốc
                var variantDeal = flashDeals.FirstOrDefault(f => f.ProductVariantId == v.Id);
                if (variantDeal != null) { result[v.Id] = variantDeal.SalePrice; continue; }

                var productDeal = flashDeals.FirstOrDefault(f =>
                    f.ProductId == v.ProductId && !f.ProductVariantId.HasValue);
                result[v.Id] = productDeal?.SalePrice ?? v.Price;
            }
            return result;
        }

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

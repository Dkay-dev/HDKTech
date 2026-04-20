using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Services
{
    /// <summary>
    /// Background job chạy mỗi 5 phút, expire các PendingCheckout quá hạn.
    /// Đăng ký trong Program.cs: builder.Services.AddHostedService&lt;ExpireCheckoutJob&gt;()
    /// </summary>
    public class ExpireCheckoutJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpireCheckoutJob> _logger;

        public ExpireCheckoutJob(IServiceScopeFactory scopeFactory, ILogger<ExpireCheckoutJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExpireCheckoutJob started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                try
                {
                    await ExpirePendingCheckoutsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng KHÔNG throw — để loop tiếp tục chạy
                    _logger.LogError(ex, "Lỗi khi expire PendingCheckouts.");
                }
            }

            _logger.LogInformation("ExpireCheckoutJob stopped.");
        }

        private async Task ExpirePendingCheckoutsAsync(CancellationToken ct)
        {
            // Tạo scope mới vì DbContext là Scoped service, không thể inject vào Singleton
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HDKTechContext>();

            var now = DateTime.UtcNow;

            var expired = await context.PendingCheckouts
                .Where(p => p.Status == CheckoutStatus.Pending && p.ExpiresAt < now)
                .ToListAsync(ct);

            if (!expired.Any()) return;

            foreach (var checkout in expired)
                checkout.Status = CheckoutStatus.Expired;

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Expired {Count} PendingCheckouts at {Time}.", expired.Count, now);
        }
    }
}

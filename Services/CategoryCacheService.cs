// Services/CategoryCacheService.cs — Module D: Thread-safe cache với SemaphoreSlim
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Services
{
    public interface ICategoryCacheService
    {
        /// <summary>Trả về list ID của category gốc + toàn bộ con/cháu. O(n) in-memory.</summary>
        List<int> GetDescendantCategoryIds(int categoryId);

        /// <summary>Xóa cache để force reload (gọi sau khi Create/Update/Delete category).</summary>
        void InvalidateCache();

        /// <summary>Preload cache (gọi lúc startup).</summary>
        Task LoadCacheAsync();
    }

    /// <summary>
    /// Module D — Thread-safe category cache.
    ///
    /// Vấn đề cũ: IMemoryCache.GetOrCreate() có thể cho nhiều thread cùng lúc
    /// bypass check và tất cả cùng query DB (thundering herd).
    ///
    /// Fix: SemaphoreSlim(1,1) + double-check pattern đảm bảo chỉ 1 thread
    /// được query DB. Cache tự hết hạn sau 10 phút qua Timer.
    /// </summary>
    public class CategoryCacheService : ICategoryCacheService, IDisposable
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        private readonly IServiceScopeFactory             _scopeFactory;
        private readonly ILogger<CategoryCacheService>    _logger;

        // ── Internal cache state (không dùng IMemoryCache để kiểm soát tốt hơn) ──
        private Dictionary<int, List<int>>? _tree;
        private readonly SemaphoreSlim       _semaphore = new SemaphoreSlim(1, 1);
        private Timer?                       _expiryTimer;
        private readonly object              _timerLock = new object();

        public CategoryCacheService(
            IServiceScopeFactory          scopeFactory,
            ILogger<CategoryCacheService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public List<int> GetDescendantCategoryIds(int categoryId)
        {
            // Fast path: nếu cache đã có thì trả về ngay (không await)
            var tree = _tree;
            if (tree == null)
            {
                // Cache chưa có — trigger load đồng bộ (blocking) lần đầu
                // Vì method là sync, ta dùng GetAwaiter().GetResult() đặc biệt ở đây.
                // Trong production, nên preload tại startup để tránh trường hợp này.
                tree = GetOrBuildTreeAsync().GetAwaiter().GetResult();
            }

            var result = new List<int> { categoryId };
            CollectDescendants(tree, categoryId, result);
            return result;
        }

        public void InvalidateCache()
        {
            _tree = null;
            StopExpiryTimer();
            _logger.LogInformation("[CategoryCache] Cache đã bị xóa (invalidated).");
        }

        public async Task LoadCacheAsync()
        {
            _tree = null;
            StopExpiryTimer();
            await GetOrBuildTreeAsync();
            _logger.LogInformation("[CategoryCache] Cache đã được preload.");
        }

        // ── Private: Build tree với SemaphoreSlim double-check ───────────────

        private async Task<Dictionary<int, List<int>>> GetOrBuildTreeAsync()
        {
            // Lần 1 check — không cần lock (volatile read)
            if (_tree != null) return _tree;

            await _semaphore.WaitAsync();
            try
            {
                // Lần 2 check SAU khi có lock (double-check pattern)
                // Tránh trường hợp nhiều thread đều đợi lock, cái đầu build xong,
                // các cái sau vào lại build lần nữa.
                if (_tree != null) return _tree;

                _logger.LogInformation("[CategoryCache] Building category tree từ DB...");

                using var scope   = _scopeFactory.CreateScope();
                var context       = scope.ServiceProvider.GetRequiredService<HDKTechContext>();

                var allCategories = await context.Categories
                    .AsNoTracking()
                    .Select(c => new { c.Id, c.ParentCategoryId })
                    .ToListAsync();

                var tree = new Dictionary<int, List<int>>();
                foreach (var cat in allCategories)
                {
                    var parentId = cat.ParentCategoryId ?? 0;
                    if (!tree.ContainsKey(parentId))
                        tree[parentId] = new List<int>();
                    tree[parentId].Add(cat.Id);
                }

                _tree = tree;
                ScheduleExpiry();

                _logger.LogInformation(
                    "[CategoryCache] Tree built: {Count} categories. Hết hạn sau {Min} phút.",
                    allCategories.Count, CacheDuration.TotalMinutes);

                return _tree;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // ── Timer-based expiry ────────────────────────────────────────────────

        private void ScheduleExpiry()
        {
            lock (_timerLock)
            {
                _expiryTimer?.Dispose();
                _expiryTimer = new Timer(_ =>
                {
                    _tree = null;
                    _logger.LogDebug("[CategoryCache] Cache hết hạn sau {Min} phút, sẽ rebuild lần sau.",
                        CacheDuration.TotalMinutes);
                }, null, CacheDuration, Timeout.InfiniteTimeSpan);
            }
        }

        private void StopExpiryTimer()
        {
            lock (_timerLock)
            {
                _expiryTimer?.Dispose();
                _expiryTimer = null;
            }
        }

        // ── DFS helper ────────────────────────────────────────────────────────

        private static void CollectDescendants(
            Dictionary<int, List<int>> tree,
            int parentId,
            List<int> result)
        {
            if (!tree.TryGetValue(parentId, out var children)) return;
            foreach (var childId in children)
            {
                result.Add(childId);
                CollectDescendants(tree, childId, result); // DFS đệ quy
            }
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose()
        {
            StopExpiryTimer();
            _semaphore.Dispose();
        }
    }
}

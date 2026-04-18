// Services/CategoryCacheService.cs
using HDKTech.Data;
using HDKTech.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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

    public class CategoryCacheService : ICategoryCacheService
    {
        private const string CacheKey = "category_tree";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CategoryCacheService> _logger;

        public CategoryCacheService(
            IMemoryCache cache,
            IServiceScopeFactory scopeFactory,
            ILogger<CategoryCacheService> logger)
        {
            _cache = cache;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public List<int> GetDescendantCategoryIds(int categoryId)
        {
            var tree = GetOrBuildTree();
            var result = new List<int> { categoryId };

            // DFS in-memory — không đụng database
            CollectDescendants(tree, categoryId, result);
            return result;
        }

        public void InvalidateCache()
        {
            _cache.Remove(CacheKey);
            _logger.LogInformation("Category cache invalidated.");
        }

        public async Task LoadCacheAsync()
        {
            _cache.Remove(CacheKey); // xóa cũ nếu có
            await Task.Run(() => GetOrBuildTree()); // build mới
            _logger.LogInformation("Category cache preloaded.");
        }

        // ── Private helpers ──────────────────────────────────────────────

        /// <summary>
        /// Dictionary: parentId → List of child IDs.
        /// Build 1 lần từ DB, cache lại.
        /// </summary>
        private Dictionary<int, List<int>> GetOrBuildTree()
        {
            return _cache.GetOrCreate(CacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                entry.Priority = CacheItemPriority.High;

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<HDKTechContext>();

                // 1 query duy nhất — lấy toàn bộ categories
                var allCategories = context.Categories
                    .AsNoTracking()
                    .Select(c => new { c.Id, c.ParentCategoryId })
                    .ToList();

                var tree = new Dictionary<int, List<int>>();
                foreach (var cat in allCategories)
                {
                    var parentId = cat.ParentCategoryId ?? 0;
                    if (!tree.ContainsKey(parentId))
                        tree[parentId] = new List<int>();
                    tree[parentId].Add(cat.Id);
                }

                _logger.LogInformation(
                    "Category tree built: {Count} categories loaded into cache.",
                    allCategories.Count);

                return tree;
            })!;
        }

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
    }
}
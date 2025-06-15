using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TBD.Shared.Repositories;

namespace TBD.Shared.CachingConfiguration;

public class CachingRepositoryDecorator<T>(
    IGenericRepository<T> inner,
    IMemoryCache cache,
    IOptions<CacheOptions>? options = null,
    ILogger<CachingRepositoryDecorator<T>>? logger = null)
    : IGenericRepository<T>
    where T : class
{
    private readonly CacheOptions _options = options?.Value ?? new CacheOptions();

    // Thread-safe collection to track cached keys for efficient invalidation
    private readonly ConcurrentHashSet<string> _cachedKeys = new();

    // Cached reflection info for performance
    private static readonly Lazy<PropertyInfo?> IdProperty = new(() =>
        typeof(T).GetProperties()
            .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                                 p.GetCustomAttribute<KeyAttribute>() != null));

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        if (!_options.EnableCaching)
            return await inner.GetAllAsync();

        var cacheKey = GenerateCacheKey("All");

        if (cache.TryGetValue(cacheKey, out IEnumerable<T>? cached) && cached != null)
        {
            logger?.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        logger?.LogDebug("Cache miss for {CacheKey}", cacheKey);
        var result = await inner.GetAllAsync();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.GetAllCacheDuration,
            Priority = CacheItemPriority.Normal,
            Size = 1
        };

        cacheOptions.RegisterPostEvictionCallback(OnCacheItemEvicted);

        var allAsync = result as T[] ?? result.ToArray();
        SetCache(cacheKey, allAsync, cacheOptions);
        return allAsync;
    }

    public async Task<T> GetByIdAsync(Guid id)
    {
        if (!_options.EnableCaching)
            return await inner.GetByIdAsync(id);

        var cacheKey = GenerateCacheKey("Id", id.ToString());

        if (cache.TryGetValue(cacheKey, out T? cached) && cached != null)
        {
            logger?.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        logger?.LogDebug("Cache miss for {CacheKey}", cacheKey);
        var result = await inner.GetByIdAsync(id);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.GetByIdCacheDuration,
            Priority = CacheItemPriority.High, // Individual items are more valuable
            Size = 1
        };

        cacheOptions.RegisterPostEvictionCallback(OnCacheItemEvicted);
        SetCache(cacheKey, result, cacheOptions);

        return result;
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        // Complex queries with expressions are harder to cache effectively
        // Consider implementing caching for specific common predicates if needed
        logger?.LogDebug("FindAsync called - bypassing cache for expression: {Expression}", predicate);
        return await inner.FindAsync(predicate);
    }

    public async Task AddAsync(T entity)
    {
        await inner.AddAsync(entity);
        await InvalidateCacheAsync(entity, "Add");
    }

    public async Task UpdateAsync(T entity)
    {
        await inner.UpdateAsync(entity);
        await InvalidateCacheAsync(entity, "Update");
    }

    public async Task DeleteAsync(T entity)
    {
        await inner.DeleteAsync(entity);
        await InvalidateCacheAsync(entity, "Delete");
    }

    private void SetCache<TValue>(string key, TValue value, MemoryCacheEntryOptions? options = null)
    {
        options ??= new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.DefaultCacheDuration, Size = 1
        };

        cache.Set(key, value, options);
        _cachedKeys.Add(key);

        logger?.LogDebug("Cached item with key: {CacheKey}, expires in: {Duration}",
            key, options.AbsoluteExpirationRelativeToNow);
    }

    private async Task InvalidateCacheAsync(T entity, string operation)
    {
        if (!_options.EnableCaching) return;

        // Always invalidate "All" cache
        var allKey = GenerateCacheKey("All");
        RemoveFromCache(allKey);

        // Try to invalidate specific entity cache if we can get its ID
        var entityId = GetEntityId(entity);
        if (entityId != null)
        {
            var idKey = GenerateCacheKey("Id", entityId.ToString()!);
            RemoveFromCache(idKey);
        }

        // For extensive invalidation, you might want to invalidate related caches
        // This could be extended based on your domain relationships

        logger?.LogDebug("Cache invalidated for {EntityType} after {Operation}. Entity ID: {EntityId}",
            typeof(T).Name, operation, entityId);

        await Task.CompletedTask; // Placeholder for any async invalidation logic
    }

    private void RemoveFromCache(string key)
    {
        cache.Remove(key);
        _cachedKeys.TryRemove(key);
        logger?.LogDebug("Removed cache key: {CacheKey}", key);
    }

    private string GenerateCacheKey(params string[] parts)
    {
        var entityName = typeof(T).Name;
        var allParts = new[] { _options.CacheKeyPrefix, entityName }.Concat(parts);
        return string.Join("_", allParts);
    }

    private object? GetEntityId(T entity)
    {
        try
        {
            return IdProperty.Value?.GetValue(entity);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to get ID from entity {EntityType}", typeof(T).Name);
            return null;
        }
    }

    private void OnCacheItemEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (key is string keyStr)
        {
            _cachedKeys.TryRemove(keyStr);
            logger?.LogDebug("Cache item evicted: {Key}, Reason: {Reason}", keyStr, reason);
        }
    }

    // Helper method for bulk cache invalidation (useful for testing or admin operations)
    public void ClearAllCache()
    {
        var keysToRemove = _cachedKeys.ToList();
        foreach (var key in keysToRemove)
        {
            cache.Remove(key);
        }

        _cachedKeys.Clear();
        logger?.LogInformation("Cleared all cache for {EntityType}", typeof(T).Name);
    }
}

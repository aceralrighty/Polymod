using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StockPredictionService.CrossCutting.Repositories;
using StockPredictionService.CrossCutting.Repositories.Configuration;

namespace StockPredictionService.CrossCutting.CachingConfiguration;

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

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
    {
        if (!_options.EnableCaching)
            return await inner.GetAllAsync(ct);

        var cacheKey = GenerateCacheKey("All");

        if (cache.TryGetValue(cacheKey, out IEnumerable<T>? cached) && cached != null)
        {
            logger?.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        logger?.LogDebug("Cache miss for {CacheKey}", cacheKey);
        var result = await inner.GetAllAsync(ct);

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

    public async Task<List<T>> GetAllChunkedAsync(int chunkSize, CancellationToken ct = default)
    {
        if (!_options.EnableCaching)
            return await inner.GetAllChunkedAsync(chunkSize, ct);

        var cacheKey = GenerateCacheKey("AllChunked", chunkSize.ToString());

        if (cache.TryGetValue(cacheKey, out List<T>? cached) && cached != null)
        {
            logger?.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        logger?.LogDebug("Cache miss for {CacheKey}", cacheKey);
        var result = await inner.GetAllChunkedAsync(chunkSize, ct);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.GetAllCacheDuration,
            Priority = CacheItemPriority.Normal,
            Size = Math.Max(1, result.Count / 1000) // Size based on result count
        };

        cacheOptions.RegisterPostEvictionCallback(OnCacheItemEvicted);
        SetCache(cacheKey, result, cacheOptions);

        return result;
    }

    public async Task<List<T>> GetAllOptimizedAsync(CancellationToken ct = default)
    {
        if (!_options.EnableCaching)
            return await inner.GetAllOptimizedAsync(ct);

        var cacheKey = GenerateCacheKey("AllOptimized");

        if (cache.TryGetValue(cacheKey, out List<T>? cached) && cached != null)
        {
            logger?.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        logger?.LogDebug("Cache miss for {CacheKey}", cacheKey);
        var result = await inner.GetAllOptimizedAsync(ct);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.GetAllCacheDuration,
            Priority = CacheItemPriority.High, // Optimized queries are valuable
            Size = Math.Max(1, result.Count / 1000)
        };

        cacheOptions.RegisterPostEvictionCallback(OnCacheItemEvicted);
        SetCache(cacheKey, result, cacheOptions);

        return result;
    }

    public async IAsyncEnumerable<T> GetAllStreamingAsync(int bufferSize, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Streaming methods are inherently designed for memory efficiency and real-time data
        // Caching defeats the purpose, so we bypass cache and delegate directly
        logger?.LogDebug("GetAllStreamingAsync called - bypassing cache for streaming operation");

        await foreach (var item in inner.GetAllStreamingAsync(bufferSize, ct))
        {
            yield return item;
        }
    }

    public async Task<List<T>> GetAllParallelAsync(int partitionCount, CancellationToken ct = default)
    {
        if (!_options.EnableCaching)
            return await inner.GetAllParallelAsync(partitionCount, ct);

        var cacheKey = GenerateCacheKey("AllParallel", partitionCount.ToString());

        if (cache.TryGetValue(cacheKey, out List<T>? cached) && cached != null)
        {
            logger?.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        logger?.LogDebug("Cache miss for {CacheKey}", cacheKey);
        var result = await inner.GetAllParallelAsync(partitionCount, ct);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.GetAllCacheDuration,
            Priority = CacheItemPriority.High, // Parallel queries are expensive
            Size = Math.Max(1, result.Count / 1000)
        };

        cacheOptions.RegisterPostEvictionCallback(OnCacheItemEvicted);
        SetCache(cacheKey, result, cacheOptions);

        return result;
    }


    public async IAsyncEnumerable<T> GetAllMemoryMappedAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var cacheKey = GenerateCacheKey("AllMemoryMapped");

        if (_options.EnableCaching && cache.TryGetValue(cacheKey, out List<T>? cached) && cached != null)
        {
            logger?.LogDebug("Cache hit for {CacheKey}", cacheKey);
            foreach (var item in cached)
            {
                yield return item;
            }

            yield break; // Exit after yielding cached items
        }

        logger?.LogDebug("Cache miss for {CacheKey}", cacheKey);

        // We have to collect the stream into a list to cache it
        var resultList = new List<T>();

        await foreach (var item in inner.GetAllMemoryMappedAsync(ct))
        {
            resultList.Add(item);
            yield return item; // Pass the item through to the caller immediately
        }

        if (!_options.EnableCaching || resultList.Count <= 0)
        {
            yield break;
        }

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.GetAllCacheDuration,
            Priority = CacheItemPriority.High,
            Size = Math.Max(1, resultList.Count / 1000)
        };

        cacheOptions.RegisterPostEvictionCallback(OnCacheItemEvicted);
        SetCache(cacheKey, resultList, cacheOptions);
    }

    public async Task<List<T>> GetAllConfigurableAsync(QueryOptions options, CancellationToken ct = default)
    {
        if (!_options.EnableCaching)
            return await inner.GetAllConfigurableAsync(options, ct);

        // Create cache key based on strategy and relevant parameters
        var strategyParams = options.Strategy switch
        {
            QueryStrategy.Chunked => options.ChunkSize.ToString(),
            QueryStrategy.Parallel => options.ParallelPartitions.ToString(),
            QueryStrategy.Streaming => options.StreamingBufferSize.ToString(),
            QueryStrategy.MemoryMapped => "default",
            _ => "standard"
        };

        var cacheKey = GenerateCacheKey("AllConfigurable", options.Strategy.ToString(), strategyParams);

        // Skip caching for streaming strategy as it's designed for memory efficiency
        if (options.Strategy == QueryStrategy.Streaming)
        {
            logger?.LogDebug("GetAllConfigurableAsync with Streaming strategy - bypassing cache");
            return await inner.GetAllConfigurableAsync(options, ct);
        }

        if (cache.TryGetValue(cacheKey, out List<T>? cached) && cached != null)
        {
            logger?.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        logger?.LogDebug("Cache miss for {CacheKey}", cacheKey);
        var result = await inner.GetAllConfigurableAsync(options, ct);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _options.GetAllCacheDuration,
            Priority = options.Strategy == QueryStrategy.Standard
                ? CacheItemPriority.Normal
                : CacheItemPriority.High, // High-performance strategies get higher priority
            Size = Math.Max(1, result.Count / 1000)
        };

        cacheOptions.RegisterPostEvictionCallback(OnCacheItemEvicted);
        SetCache(cacheKey, result, cacheOptions);

        return result;
    }

    public async Task BulkInsertAsync(IEnumerable<T> entities)
    {
        await inner.BulkInsertAsync(entities);
    }

    public async Task<T> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!_options.EnableCaching)
            return await inner.GetByIdAsync(id, ct);

        var cacheKey = GenerateCacheKey("Id", id.ToString());

        if (cache.TryGetValue(cacheKey, out T? cached) && cached != null)
        {
            logger?.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        logger?.LogDebug("Cache miss for {CacheKey}", cacheKey);
        var result = await inner.GetByIdAsync(id, ct);

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

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        // Complex queries with expressions are harder to cache effectively
        // Consider implementing caching for specific common predicates if needed
        logger?.LogDebug("FindAsync called - bypassing cache for expression: {Expression}", predicate);
        return await inner.FindAsync(predicate, ct);
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

    private Task InvalidateCacheAsync(T entity, string operation)
    {
        if (!_options.EnableCaching)
            return Task.CompletedTask;

        var entityName = typeof(T).Name;
        var entityId = GetEntityId(entity);
        var invalidatedKeys = new List<string>();

        // Create the prefix to match against
        var allKeysPrefix = $"{_options.CacheKeyPrefix}_{entityName}_All";

        // Invalidate all "GetAll*" related caches since the dataset has changed
        var keysSnapshot = _cachedKeys.ToList();

        foreach (var key in keysSnapshot)
        {
            if (!key.StartsWith(allKeysPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            RemoveFromCache(key);
            invalidatedKeys.Add(key);
        }

        // Invalidate specific entity cache if we can get its ID
        if (entityId != null)
        {
            var idKey = GenerateCacheKey("Id", entityId.ToString()!);
            if (_cachedKeys.Contains(idKey))
            {
                RemoveFromCache(idKey);
                invalidatedKeys.Add(idKey);
            }
        }

        logger?.LogDebug(
            "Cache invalidated for {EntityType} after {Operation}. Entity ID: {EntityId}. Keys invalidated: {KeyCount}",
            entityName, operation, entityId, invalidatedKeys.Count);

        return Task.CompletedTask;
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
        if (key is not string keyStr)
        {
            return;
        }

        _cachedKeys.TryRemove(keyStr);
        logger?.LogDebug("Cache item evicted: {Key}, Reason: {Reason}", keyStr, reason);
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

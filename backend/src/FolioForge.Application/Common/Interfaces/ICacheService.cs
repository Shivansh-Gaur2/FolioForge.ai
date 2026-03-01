namespace FolioForge.Application.Common.Interfaces;

/// <summary>
/// Abstraction for a distributed cache service.
/// Implementations can use Redis, Memcached, or any other distributed cache provider.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key, deserializing it to <typeparamref name="T"/>.
    /// Returns default if the key does not exist.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache with an optional expiration time.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cached keys matching the given prefix pattern.
    /// Useful for cache invalidation of related entries (e.g., all portfolio data for a tenant).
    /// </summary>
    Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cached value or creates it using the factory if it doesn't exist.
    /// This is the recommended pattern for cache-aside.
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a key exists in the cache.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

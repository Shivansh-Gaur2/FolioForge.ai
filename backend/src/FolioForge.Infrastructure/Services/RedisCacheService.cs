using System.Text.Json;
using FolioForge.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;   // ← add this
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FolioForge.Infrastructure.Services;

/// <summary>
/// Redis-backed implementation of <see cref="ICacheService"/>.
/// Uses IDistributedCache for standard get/set and IConnectionMultiplexer for advanced operations.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly string _instanceName;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        IOptions<RedisCacheOptions> redisOptions)
    {
        _cache = cache;
        _redis = redis;
        _logger = logger;
        _instanceName = redisOptions.Value.InstanceName ?? string.Empty;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(key, cancellationToken);
            if (json is null)
            {
                _logger.LogDebug("Cache MISS for key: {CacheKey}", key);
                return default;
            }

            _logger.LogDebug("Cache HIT for key: {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from cache for key: {CacheKey}. Returning default.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration;
            }
            else
            {
                // Default expiration: 30 minutes
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            }

            await _cache.SetStringAsync(key, json, options, cancellationToken);
            _logger.LogDebug("Cache SET for key: {CacheKey}, TTL: {Expiration}", key, options.AbsoluteExpirationRelativeToNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error writing to cache for key: {CacheKey}. Operation skipped.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache REMOVE for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache key: {CacheKey}. Operation skipped.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var pattern = $"{_instanceName}{prefixKey}*";

            foreach (var endpoint in _redis.GetEndPoints())
            {
                var server = _redis.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica) continue;

                var database = _redis.GetDatabase();

                await foreach (var key in server.KeysAsync(pattern: pattern).WithCancellation(cancellationToken))
                {
                    await database.KeyDeleteAsync(key);
                }
            }

            _logger.LogDebug("Cache REMOVE BY PREFIX: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache keys with prefix: {Prefix}. Operation skipped.", prefixKey);
        }
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        // Try cache first
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
            return cached;

        // Cache miss — invoke factory, cache result, and return
        var value = await factory();
        if (value is not null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _cache.GetAsync(key, cancellationToken);
            return value is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking cache existence for key: {CacheKey}.", key);
            return false;
        }
    }
}

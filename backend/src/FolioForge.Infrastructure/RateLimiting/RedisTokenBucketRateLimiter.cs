using FolioForge.Application.Common.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FolioForge.Infrastructure.RateLimiting;

/// <summary>
/// Redis-backed distributed Token Bucket rate limiter.
///
/// WHY TOKEN BUCKET over Leaky Bucket or Fixed/Sliding Window?
/// ─────────────────────────────────────────────────────────────
/// • Token Bucket is the best fit when you want controlled burstiness.
///   A user idle for 10 minutes accumulates tokens (up to BucketCapacity),
///   allowing a short burst — but never exceeding the cap.
///   
/// • Leaky Bucket enforces a perfectly smooth outflow (no bursts ever),
///   which is too strict for interactive UIs where users naturally cluster actions.
///   
/// • Fixed Window has boundary spikes (2× burst at window edges).
///   Sliding Window fixes this but is more complex and still doesn't model
///   "earned burst capacity" the way Token Bucket does.
///
/// ATOMICITY
/// ─────────
/// All state mutations happen inside a single Lua script executed atomically by Redis.
/// This eliminates race conditions between concurrent requests hitting different API instances.
/// No distributed locks needed — Lua scripts in Redis are single-threaded by design.
///
/// KEY DESIGN
/// ──────────
/// Each (policyName, clientId) pair maps to a Redis hash with two fields:
///   • "tokens"     – current token count (float, to handle fractional refills)
///   • "last_refill" – Unix timestamp of last refill (float, millisecond precision)
///
/// TTL is set to 2× the time needed to fully refill the bucket, ensuring
/// idle buckets are automatically cleaned up by Redis eviction.
/// </summary>
public sealed class RedisTokenBucketRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IOptionsMonitor<RateLimiterOptions> _optionsMonitor;
    private readonly ILogger<RedisTokenBucketRateLimiter> _logger;

    /// <summary>
    /// Lua script implementing the Token Bucket algorithm atomically.
    ///
    /// ARGS:
    ///   KEYS[1] = Redis hash key for this (policy, client) bucket
    ///   ARGV[1] = bucket capacity (max tokens)
    ///   ARGV[2] = refill rate (tokens per interval)
    ///   ARGV[3] = refill interval in seconds
    ///   ARGV[4] = current Unix timestamp (seconds, with millisecond precision)
    ///   ARGV[5] = TTL for the key in seconds
    ///
    /// RETURNS: { allowed (0/1), remaining_tokens, retry_after_seconds }
    /// </summary>
    private static readonly LuaScript TokenBucketScript = LuaScript.Prepare(
        """
        local key = @key
        local capacity = tonumber(@capacity)
        local refill_rate = tonumber(@refillRate)
        local refill_interval = tonumber(@refillInterval)
        local now = tonumber(@now)
        local ttl = tonumber(@ttl)

        -- Read current state
        local bucket = redis.call('HMGET', key, 'tokens', 'last_refill')
        local tokens = tonumber(bucket[1])
        local last_refill = tonumber(bucket[2])

        -- Initialize bucket on first request (start full → first request always succeeds)
        if tokens == nil then
            tokens = capacity
            last_refill = now
        end

        -- Calculate elapsed time and tokens to add
        local elapsed = math.max(0, now - last_refill)
        local tokens_to_add = (elapsed / refill_interval) * refill_rate

        -- Refill: add accrued tokens, cap at capacity
        tokens = math.min(capacity, tokens + tokens_to_add)
        last_refill = now

        local allowed = 0
        local remaining = math.floor(tokens)
        local retry_after = 0

        if tokens >= 1 then
            -- Consume one token
            tokens = tokens - 1
            remaining = math.floor(tokens)
            allowed = 1
        else
            -- Denied: calculate how long until 1 token is available
            local deficit = 1 - tokens
            retry_after = (deficit / refill_rate) * refill_interval
        end

        -- Persist state
        redis.call('HMSET', key, 'tokens', tostring(tokens), 'last_refill', tostring(last_refill))
        redis.call('EXPIRE', key, ttl)

        return { allowed, remaining, tostring(retry_after) }
        """);

    public RedisTokenBucketRateLimiter(
        IConnectionMultiplexer redis,
        IOptionsMonitor<RateLimiterOptions> optionsMonitor,
        ILogger<RedisTokenBucketRateLimiter> logger)
    {
        _redis = redis;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task<RateLimitResult> TryAcquireAsync(
        string policyName,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.Policies.TryGetValue(policyName, out var policyOptions))
        {
            _logger.LogWarning(
                "Rate limit policy '{PolicyName}' not found. Allowing request by default",
                policyName);
            return RateLimitResult.Allowed(limit: 0, remaining: 0);
        }

        var policy = policyOptions.ToPolicy(policyName);
        var key = BuildKey(options.KeyPrefix, policyName, clientId);

        // TTL = 2× full refill time, minimum 60 seconds
        var fullRefillSeconds = (policy.BucketCapacity / (double)policy.RefillRate) * policy.RefillInterval.TotalSeconds;
        var ttlSeconds = (int)Math.Max(60, fullRefillSeconds * 2);

        try
        {
            var db = _redis.GetDatabase();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

            var result = await db.ScriptEvaluateAsync(TokenBucketScript, new
            {
                key = (RedisKey)key,
                capacity = policy.BucketCapacity,
                refillRate = policy.RefillRate,
                refillInterval = policy.RefillInterval.TotalSeconds,
                now,
                ttl = ttlSeconds
            });

            var results = (RedisResult[])result!;
            var allowed = (int)results[0] == 1;
            var remaining = (int)results[1];
            var retryAfter = double.Parse((string)results[2]!);

            if (!allowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for client '{ClientId}' on policy '{PolicyName}'. Retry after {RetryAfter:F2}s",
                    clientId, policyName, retryAfter);
            }

            return allowed
                ? RateLimitResult.Allowed(policy.BucketCapacity, remaining)
                : RateLimitResult.Denied(policy.BucketCapacity, retryAfter);
        }
        catch (RedisConnectionException ex)
        {
            // FAIL-OPEN: If Redis is down, allow the request.
            // Rate limiting should never take down the entire service.
            // Log at Error level so ops can respond quickly.
            _logger.LogError(ex,
                "Redis unavailable during rate limit check for '{ClientId}' on '{PolicyName}'. Failing open (allowing request)",
                clientId, policyName);
            return RateLimitResult.Allowed(policy.BucketCapacity, remaining: policy.BucketCapacity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during rate limit check for '{ClientId}' on '{PolicyName}'. Failing open",
                clientId, policyName);
            return RateLimitResult.Allowed(policy.BucketCapacity, remaining: policy.BucketCapacity);
        }
    }

    /// <summary>
    /// Builds a Redis key: {prefix}:{policyName}:{clientId}
    /// Example: "rl:Default:user_abc123" or "rl:Auth:ip_192.168.1.1"
    /// </summary>
    private static string BuildKey(string prefix, string policyName, string clientId)
        => $"{prefix}:{policyName}:{clientId}";
}

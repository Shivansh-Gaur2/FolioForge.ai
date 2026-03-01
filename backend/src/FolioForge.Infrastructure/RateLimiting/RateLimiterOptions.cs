using FolioForge.Application.Common.RateLimiting;

namespace FolioForge.Infrastructure.RateLimiting;

/// <summary>
/// Configuration options for the distributed rate limiter.
/// Bound from appsettings.json section "RateLimiting".
/// </summary>
public sealed class RateLimiterOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Master switch to enable/disable rate limiting globally.
    /// Useful for disabling in integration tests or during incident response.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Redis key prefix for all rate-limit buckets.
    /// Keeps rate-limit keys separated from cache keys in the same Redis instance.
    /// </summary>
    public string KeyPrefix { get; set; } = "rl";

    /// <summary>
    /// Named policies. Each policy defines a Token Bucket configuration.
    /// Keys are policy names (case-insensitive), values are the bucket parameters.
    /// </summary>
    public Dictionary<string, PolicyOptions> Policies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// IP addresses or CIDR ranges that bypass rate limiting entirely.
    /// Useful for health checks, internal services, and monitoring.
    /// </summary>
    public List<string> Whitelist { get; set; } = [];

    /// <summary>
    /// Whether to include rate-limit headers (RateLimit-Limit, RateLimit-Remaining, etc.)
    /// in ALL responses, not just 429s. Helps clients self-regulate.
    /// </summary>
    public bool IncludeHeadersOnSuccess { get; set; } = true;
}

/// <summary>
/// Configuration for a single rate-limit policy.
/// Maps to a <see cref="RateLimitPolicy"/> at runtime.
/// </summary>
public sealed class PolicyOptions
{
    /// <summary>Maximum tokens (burst ceiling).</summary>
    public int BucketCapacity { get; set; } = 20;

    /// <summary>Tokens added per refill interval.</summary>
    public int RefillRate { get; set; } = 10;

    /// <summary>Refill interval in seconds.</summary>
    public double RefillIntervalSeconds { get; set; } = 1.0;

    public RateLimitPolicy ToPolicy(string name) => new()
    {
        Name = name,
        BucketCapacity = BucketCapacity,
        RefillRate = RefillRate,
        RefillInterval = TimeSpan.FromSeconds(RefillIntervalSeconds)
    };
}

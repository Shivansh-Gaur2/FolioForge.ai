namespace FolioForge.Application.Common.RateLimiting;

/// <summary>
/// Defines a rate-limiting policy using the Token Bucket algorithm.
/// 
/// The Token Bucket naturally handles "burstiness":
///   - Tokens accumulate at <see cref="RefillRate"/> tokens per <see cref="RefillInterval"/>.
///   - A user who hasn't been active accumulates tokens up to <see cref="BucketCapacity"/>.
///   - They can then "burst" up to that capacity before being throttled.
///   - Sustained throughput is capped at RefillRate/RefillInterval (e.g., 10 tokens per second).
///
/// Example — "Standard" policy:
///   BucketCapacity = 20, RefillRate = 10, RefillInterval = 1s
///   → Sustained: 10 req/s. Burst: up to 20 requests instantly, then back to 10/s.
///
/// Example — "Strict" policy for auth endpoints:
///   BucketCapacity = 5, RefillRate = 2, RefillInterval = 1s
///   → Sustained: 2 req/s. Burst: up to 5, then throttled hard.
/// </summary>
public sealed record RateLimitPolicy
{
    /// <summary>
    /// Unique name for this policy (e.g., "Default", "Auth", "Upload").
    /// Used as the Redis key namespace and for attribute-based routing.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Maximum number of tokens the bucket can hold.
    /// This is the upper bound on burst size — even if a user is idle all day,
    /// they can never send more than this many requests in a single burst.
    /// </summary>
    public required int BucketCapacity { get; init; }

    /// <summary>
    /// Number of tokens added to the bucket per <see cref="RefillInterval"/>.
    /// This defines the sustained throughput rate.
    /// </summary>
    public required int RefillRate { get; init; }

    /// <summary>
    /// How often tokens are refilled. Combined with <see cref="RefillRate"/>,
    /// this determines the sustained request rate.
    /// </summary>
    public required TimeSpan RefillInterval { get; init; }

    /// <summary>
    /// Computed: sustained requests per second = RefillRate / RefillInterval.TotalSeconds
    /// </summary>
    public double SustainedRatePerSecond => RefillRate / RefillInterval.TotalSeconds;
}

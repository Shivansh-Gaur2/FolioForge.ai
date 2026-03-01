namespace FolioForge.Application.Common.RateLimiting;

/// <summary>
/// Outcome of a rate-limit check against a Token Bucket.
/// Provides all data needed for the middleware to build proper HTTP 429 responses
/// with standard rate-limit headers (RateLimit-Limit, RateLimit-Remaining, Retry-After).
/// </summary>
public sealed record RateLimitResult
{
    /// <summary>Whether the request is allowed (tokens were available).</summary>
    public required bool IsAllowed { get; init; }

    /// <summary>Bucket capacity (maps to RateLimit-Limit header).</summary>
    public required int Limit { get; init; }

    /// <summary>Tokens remaining after this request (maps to RateLimit-Remaining header).</summary>
    public required int Remaining { get; init; }

    /// <summary>
    /// Seconds until the bucket has at least 1 token again.
    /// Only meaningful when <see cref="IsAllowed"/> is false (maps to Retry-After header).
    /// </summary>
    public required double RetryAfterSeconds { get; init; }

    public static RateLimitResult Allowed(int limit, int remaining) => new()
    {
        IsAllowed = true,
        Limit = limit,
        Remaining = remaining,
        RetryAfterSeconds = 0
    };

    public static RateLimitResult Denied(int limit, double retryAfterSeconds) => new()
    {
        IsAllowed = false,
        Limit = limit,
        Remaining = 0,
        RetryAfterSeconds = retryAfterSeconds
    };
}

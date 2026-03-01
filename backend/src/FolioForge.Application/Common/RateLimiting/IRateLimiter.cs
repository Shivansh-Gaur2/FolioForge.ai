namespace FolioForge.Application.Common.RateLimiting;

/// <summary>
/// Distributed rate limiter contract.
/// 
/// Implementations must be thread-safe and cluster-safe — multiple API instances
/// must share the same backing store (e.g., Redis) so that a single user/IP
/// cannot bypass limits by hitting different instances.
/// 
/// The <paramref name="clientId"/> is the discriminator: it can be a User ID (from JWT),
/// an IP address, an API key, or any unique identifier for the caller.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Attempts to consume one token from the bucket identified by
    /// (<paramref name="policyName"/>, <paramref name="clientId"/>).
    /// 
    /// This operation must be atomic — in Redis, this means using Lua scripts
    /// to avoid race conditions between concurrent requests.
    /// </summary>
    /// <param name="policyName">The rate-limit policy to apply (e.g., "Default", "Auth").</param>
    /// <param name="clientId">Unique identifier for the client (User ID, IP, API key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether the request is allowed and remaining quota.</returns>
    Task<RateLimitResult> TryAcquireAsync(
        string policyName,
        string clientId,
        CancellationToken cancellationToken = default);
}

namespace FolioForge.Application.Common;

/// <summary>
/// Centralized cache key definitions. All Redis keys flow through here
/// to ensure consistency and make invalidation predictable.
/// 
/// Key format: {Entity}:{Identifier}:{SubEntity?}
/// The "FolioForge:" instance prefix is prepended automatically by Redis config.
/// </summary>
public static class CacheKeys
{
    // ── Portfolio ──
    public static string PortfolioById(Guid id) => $"portfolio:{id}";
    public static string PortfoliosByUser(Guid userId) => $"user:{userId}:portfolios";
    public static string PortfolioBySlug(string slug) => $"portfolio:slug:{slug}";

    // ── Tenant ──
    public static string TenantById(Guid id) => $"tenant:id:{id}";
    public static string TenantByIdentifier(string identifier) => $"tenant:ident:{identifier}";

    // ── Invalidation prefixes ──
    /// <summary>Invalidate all cached data for a specific portfolio.</summary>
    public static string PortfolioPrefix(Guid id) => $"portfolio:{id}";
    /// <summary>Invalidate all cached data for a specific user.</summary>
    public static string UserPrefix(Guid userId) => $"user:{userId}";

    // ── Cache Durations ──
    public static readonly TimeSpan PortfolioTtl = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan TenantTtl = TimeSpan.FromHours(1);
    public static readonly TimeSpan UserPortfolioListTtl = TimeSpan.FromMinutes(10);
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace FolioForge.Infrastructure.RateLimiting;

/// <summary>
/// Resolves the client identity for rate limiting.
///
/// Resolution strategy (ordered by specificity):
///   1. Authenticated user → User ID from JWT "sub" claim (most precise)
///   2. API key            → From X-Api-Key header (future-proofing)
///   3. IP address         → Forwarded-For or connection remote IP (fallback)
///
/// This hierarchy ensures:
///   - Authenticated users are tracked individually regardless of IP
///   - Anonymous scrapers are tracked by IP  
///   - Users behind shared NATs/proxies get their own bucket when authenticated
/// </summary>
public interface IClientIdentityResolver
{
    /// <summary>
    /// Resolves a unique, stable identifier for the current HTTP request's client.
    /// </summary>
    string Resolve(HttpContext context);
}

public sealed class ClientIdentityResolver : IClientIdentityResolver
{
    private readonly ILogger<ClientIdentityResolver> _logger;

    public ClientIdentityResolver(ILogger<ClientIdentityResolver> logger)
    {
        _logger = logger;
    }

    public string Resolve(HttpContext context)
    {
        // Strategy 1: Authenticated user — extract "sub" claim
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? context.User.FindFirstValue("sub");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        // Strategy 2: API key header (future-proofing for B2B integrations)
        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return $"apikey:{apiKey}";
        }

        // Strategy 3: IP address (handles proxies via X-Forwarded-For)
        var ip = ResolveIpAddress(context);
        return $"ip:{ip}";
    }

    private string ResolveIpAddress(HttpContext context)
    {
        // Check X-Forwarded-For first (when behind a load balancer/reverse proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs: "client, proxy1, proxy2"
            // The leftmost is the original client IP
            var clientIp = forwardedFor.Split(',', StringSplitOptions.TrimEntries)[0];
            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                return clientIp;
            }
        }

        // Direct connection IP
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            return remoteIp;
        }

        _logger.LogWarning("Could not resolve client IP address. Using 'unknown' for rate limiting");
        return "unknown";
    }
}

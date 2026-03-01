using FolioForge.Application.Common;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace FolioForge.Infrastructure.Middleware
{
    /// <summary>
    /// Lightweight record for caching tenant data in Redis.
    /// We can't cache the EF entity directly (private setters, navigation props).
    /// </summary>
    internal record CachedTenant(Guid Id, string Identifier, bool IsActive);

    /// <summary>
    /// Middleware that resolves the current tenant. Resolution order:
    /// 1. JWT "tenantId" claim (for authenticated users — no header needed)
    /// 2. X-Tenant-Id header (for unauthenticated / public endpoints)
    /// 
    /// Tenant lookups are cached in Redis to avoid hitting the DB on every request.
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        /// <summary>
        /// Paths that don't require tenant resolution.
        /// Auth endpoints handle their own tenant logic internally.
        /// </summary>
        private static readonly string[] ExcludedPaths = new[]
        {
            "/api/tenants",
            "/api/auth",
            "/swagger",
            "/health"
        };

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // Skip tenant resolution for excluded paths
            if (ExcludedPaths.Any(p => path.StartsWith(p)))
            {
                await _next(context);
                return;
            }

            var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
            var cache = context.RequestServices.GetRequiredService<ICacheService>();

            // ── Strategy 1: Try to resolve tenant from JWT Bearer token ──
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var tokenString = authHeader.Substring("Bearer ".Length).Trim();
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    if (handler.CanReadToken(tokenString))
                    {
                        var jwt = handler.ReadJwtToken(tokenString);
                        var tenantIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;

                        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim, out var tenantId))
                        {
                            var cached = await cache.GetOrSetAsync(
                                CacheKeys.TenantById(tenantId),
                                async () =>
                                {
                                    var t = await dbContext.Tenants
                                        .IgnoreQueryFilters()
                                        .FirstOrDefaultAsync(t => t.Id == tenantId);
                                    return t is null ? null! : new CachedTenant(t.Id, t.Identifier, t.IsActive);
                                },
                                CacheKeys.TenantTtl);

                            if (cached != null && cached.IsActive)
                            {
                                tenantContext.SetTenant(cached.Id, cached.Identifier);
                                _logger.LogDebug("Tenant resolved from JWT (cached): {TenantIdentifier}", cached.Identifier);
                                await _next(context);
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse JWT for tenant resolution, falling back to header.");
                }
            }

            // ── Strategy 2: Fall back to X-Tenant-Id header ──
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) &&
                !string.IsNullOrWhiteSpace(tenantHeader))
            {
                var tenantIdentifier = tenantHeader.ToString().Trim().ToLowerInvariant();

                var cached = await cache.GetOrSetAsync(
                    CacheKeys.TenantByIdentifier(tenantIdentifier),
                    async () =>
                    {
                        var t = await dbContext.Tenants
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(t => t.Identifier == tenantIdentifier);
                        return t is null ? null! : new CachedTenant(t.Id, t.Identifier, t.IsActive);
                    },
                    CacheKeys.TenantTtl);

                if (cached == null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsJsonAsync(new { error = $"Tenant '{tenantIdentifier}' not found." });
                    return;
                }

                if (!cached.IsActive)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "Tenant is deactivated." });
                    return;
                }

                tenantContext.SetTenant(cached.Id, cached.Identifier);
                _logger.LogDebug("Tenant resolved from header (cached): {TenantIdentifier}", cached.Identifier);
                await _next(context);
                return;
            }

            // ── Neither JWT nor header provided ──
            _logger.LogWarning("No tenant could be resolved for path: {Path}", path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Authentication required. Please log in or provide X-Tenant-Id header." });
        }
    }
}

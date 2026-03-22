using FolioForge.Application.Common;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FolioForge.Infrastructure.Middleware
{
    /// <summary>
    /// Lightweight record for caching tenant data in Redis.
    /// We can't cache the EF entity directly (private setters, navigation props).
    /// </summary>
    internal record CachedTenant(Guid Id, string Identifier, bool IsActive);

    /// <summary>
    /// Middleware that resolves the current tenant. Resolution order:
    /// 1. Validated JWT "tenantId" claim (for authenticated users)
    /// 2. Auto-assign the default "folioforge" tenant (for public / unauthenticated requests)
    /// 
    /// IMPORTANT: This middleware MUST be placed AFTER UseAuthentication().
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
            "/api/p/",
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

            var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
            var cache = context.RequestServices.GetRequiredService<ICacheService>();

            // ── Strategy 1: Resolve tenant from VALIDATED JWT claims ──
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var tenantIdClaim = context.User.FindFirst("tenantId")?.Value;

                if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim, out var tenantId))
                {
                    var cached = await GetCachedTenantByIdAsync(cache, context, tenantId);

                    if (cached != null && cached.IsActive)
                    {
                        tenantContext.SetTenant(cached.Id, cached.Identifier);
                        await _next(context);
                        return;
                    }
                }
            }

            // ── Strategy 2: Auto-assign default tenant for unauthenticated requests ──
            var defaultTenant = await GetCachedTenantByIdAsync(
                cache, context, ApplicationDbContext.DefaultTenantId);

            if (defaultTenant != null && defaultTenant.IsActive)
            {
                tenantContext.SetTenant(defaultTenant.Id, defaultTenant.Identifier);
                await _next(context);
                return;
            }

            _logger.LogError("Default tenant not found or inactive. Run migrations to seed it.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "System configuration error." });
        }

        private static async Task<CachedTenant?> GetCachedTenantByIdAsync(
            ICacheService cache, HttpContext context, Guid tenantId)
        {
            return await cache.GetOrSetAsync(
                CacheKeys.TenantById(tenantId),
                async () =>
                {
                    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var t = await dbContext.Tenants
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(t => t.Id == tenantId);
                    return t is null ? null! : new CachedTenant(t.Id, t.Identifier, t.IsActive);
                },
                CacheKeys.TenantTtl);
        }
    }
}

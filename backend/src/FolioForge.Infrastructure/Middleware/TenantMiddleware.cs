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
    /// Middleware that resolves the current tenant. Resolution order:
    /// 1. JWT "tenantId" claim (for authenticated users — no header needed)
    /// 2. X-Tenant-Id header (for unauthenticated / public endpoints)
    /// 
    /// Must run BEFORE authentication so the DbContext query filters are set.
    /// For JWT-based resolution, we parse the token manually (before ASP.NET auth runs).
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
                            var tenant = await dbContext.Tenants
                                .IgnoreQueryFilters()
                                .FirstOrDefaultAsync(t => t.Id == tenantId);

                            if (tenant != null && tenant.IsActive)
                            {
                                tenantContext.SetTenant(tenant.Id, tenant.Identifier);
                                _logger.LogDebug("Tenant resolved from JWT: {TenantIdentifier}", tenant.Identifier);
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

                var tenant = await dbContext.Tenants
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Identifier == tenantIdentifier);

                if (tenant == null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsJsonAsync(new { error = $"Tenant '{tenantIdentifier}' not found." });
                    return;
                }

                if (!tenant.IsActive)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "Tenant is deactivated." });
                    return;
                }

                tenantContext.SetTenant(tenant.Id, tenant.Identifier);
                _logger.LogDebug("Tenant resolved from header: {TenantIdentifier}", tenant.Identifier);
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

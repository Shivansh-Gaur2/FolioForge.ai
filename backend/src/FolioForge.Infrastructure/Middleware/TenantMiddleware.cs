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
    /// Middleware that resolves the current tenant from the X-Tenant-Id header.
    /// Must run early in the pipeline, before any DbContext usage.
    /// 
    /// Flow:
    /// 1. Extract tenant identifier from X-Tenant-Id header
    /// 2. Look up tenant in database (bypassing query filters)
    /// 3. Populate ITenantContext for the rest of the request
    /// 4. Return 400 if header missing, 404 if tenant not found, 403 if inactive
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        /// <summary>
        /// Paths that don't require tenant resolution (e.g., health checks, tenant management).
        /// </summary>
        private static readonly string[] ExcludedPaths = new[]
        {
            "/api/tenants",
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

            // 1. Extract tenant identifier from header
            if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) ||
                string.IsNullOrWhiteSpace(tenantHeader))
            {
                _logger.LogWarning("Request missing X-Tenant-Id header for path: {Path}", path);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "X-Tenant-Id header is required." });
                return;
            }

            var tenantIdentifier = tenantHeader.ToString().Trim().ToLowerInvariant();

            // 2. Resolve tenant from database
            // We use the DbContext directly here (not through repository) 
            // to bypass global query filters with IgnoreQueryFilters()
            var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            var tenant = await dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Identifier == tenantIdentifier);

            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found: {TenantIdentifier}", tenantIdentifier);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { error = $"Tenant '{tenantIdentifier}' not found." });
                return;
            }

            if (!tenant.IsActive)
            {
                _logger.LogWarning("Inactive tenant attempted access: {TenantIdentifier}", tenantIdentifier);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Tenant is deactivated." });
                return;
            }

            // 3. Populate tenant context for the rest of the pipeline
            var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
            tenantContext.SetTenant(tenant.Id, tenant.Identifier);

            _logger.LogDebug("Tenant resolved: {TenantIdentifier} ({TenantId})", tenant.Identifier, tenant.Id);

            await _next(context);
        }
    }
}

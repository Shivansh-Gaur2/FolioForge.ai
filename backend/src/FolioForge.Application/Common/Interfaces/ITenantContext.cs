namespace FolioForge.Application.Common.Interfaces
{
    /// <summary>
    /// Provides the current tenant context for the request scope.
    /// Resolved by middleware from the X-Tenant-Id header.
    /// </summary>
    public interface ITenantContext
    {
        /// <summary>
        /// The database ID of the current tenant. 
        /// Throws if no tenant has been resolved for the current request.
        /// </summary>
        Guid TenantId { get; }

        /// <summary>
        /// The string identifier (slug) of the current tenant.
        /// </summary>
        string TenantIdentifier { get; }

        /// <summary>
        /// Whether a tenant has been successfully resolved for the current scope.
        /// </summary>
        bool IsResolved { get; }

        /// <summary>
        /// Sets the tenant for the current scope. Called by middleware.
        /// </summary>
        void SetTenant(Guid tenantId, string identifier);
    }
}

using FolioForge.Application.Common.Interfaces;

namespace FolioForge.Infrastructure.Services
{
    /// <summary>
    /// Scoped service that holds the tenant context for the current request.
    /// Populated by TenantMiddleware, consumed by DbContext query filters.
    /// </summary>
    public class TenantContext : ITenantContext
    {
        private Guid _tenantId;
        private string _identifier = string.Empty;

        public Guid TenantId
        {
            get
            {
                if (!IsResolved)
                    throw new InvalidOperationException("Tenant has not been resolved for the current scope.");
                return _tenantId;
            }
        }

        public string TenantIdentifier => _identifier;

        public bool IsResolved { get; private set; }

        public void SetTenant(Guid tenantId, string identifier)
        {
            _tenantId = tenantId;
            _identifier = identifier;
            IsResolved = true;
        }
    }
}

using FolioForge.Domain.Entities;

namespace FolioForge.Application.Common.Interfaces
{
    /// <summary>
    /// Repository for managing Tenant entities.
    /// Used by middleware to resolve tenants from identifiers.
    /// </summary>
    public interface ITenantRepository
    {
        Task<Tenant?> GetByIdentifierAsync(string identifier);
        Task<Tenant?> GetByIdAsync(Guid id);
        Task AddAsync(Tenant tenant);
        Task SaveChangesAsync();
    }
}

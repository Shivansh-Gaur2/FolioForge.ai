namespace FolioForge.Domain.Interfaces
{
    /// <summary>
    /// Marker interface for entities that belong to a specific tenant.
    /// EF Core global query filters use this to automatically scope queries.
    /// </summary>
    public interface ITenantEntity
    {
        Guid TenantId { get; set; }
    }
}

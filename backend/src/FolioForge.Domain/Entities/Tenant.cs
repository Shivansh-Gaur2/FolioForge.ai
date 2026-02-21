namespace FolioForge.Domain.Entities
{
    /// <summary>
    /// Represents an isolated workspace/organization.
    /// Each tenant's data is logically separated via row-level filtering.
    /// </summary>
    public class Tenant : BaseEntity
    {
        public string Name { get; private set; } = default!;

        /// <summary>
        /// URL-friendly identifier used in the X-Tenant-Id header.
        /// Example: "acme-corp", "shivansh-workspace"
        /// </summary>
        public string Identifier { get; private set; } = default!;

        public bool IsActive { get; private set; } = true;

        private Tenant() { }

        public Tenant(string name, string identifier)
        {
            Name = name;
            Identifier = identifier.ToLowerInvariant();
            IsActive = true;
        }

        public void Deactivate() => IsActive = false;
        public void Activate() => IsActive = true;
    }
}

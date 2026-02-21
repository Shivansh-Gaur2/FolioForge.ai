using FolioForge.Domain.Interfaces;

namespace FolioForge.Domain.Entities
{
    /// <summary>
    /// A user that belongs to a specific tenant.
    /// Passwords are stored as BCrypt hashes — never plain text.
    /// </summary>
    public class User : BaseEntity, ITenantEntity
    {
        public string Email { get; private set; } = default!;
        public string FullName { get; private set; } = default!;
        public string PasswordHash { get; private set; } = default!;
        public Guid TenantId { get; set; }

        private User() { }

        public User(string email, string fullName, string passwordHash, Guid tenantId)
        {
            Email = email.ToLowerInvariant();
            FullName = fullName;
            PasswordHash = passwordHash;
            TenantId = tenantId;
        }

        public void UpdateProfile(string fullName)
        {
            FullName = fullName;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

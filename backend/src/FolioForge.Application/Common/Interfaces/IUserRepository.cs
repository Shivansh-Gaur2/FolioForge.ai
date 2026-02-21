using FolioForge.Domain.Entities;

namespace FolioForge.Application.Common.Interfaces
{
    /// <summary>
    /// Repository for User entity operations.
    /// All queries are automatically tenant-scoped via EF Core query filters.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Check if email exists across ALL tenants (bypasses query filters).
        /// Used during registration to prevent duplicate emails globally.
        /// </summary>
        Task<bool> EmailExistsGloballyAsync(string email);

        Task AddAsync(User user);
        Task SaveChangesAsync();
    }
}

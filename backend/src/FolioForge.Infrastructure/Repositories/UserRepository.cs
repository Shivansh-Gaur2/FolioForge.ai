using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Looks up a user by email across ALL tenants (bypasses query filters).
        /// Used during login when the tenant context is not yet resolved.
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        }

        /// <summary>
        /// Checks email existence across ALL tenants (bypasses query filters).
        /// This prevents the same email from registering in multiple tenants.
        /// </summary>
        public async Task<bool> EmailExistsGloballyAsync(string email)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == email.ToLowerInvariant());
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

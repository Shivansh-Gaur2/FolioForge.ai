using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for Tenant entity. 
    /// Uses IgnoreQueryFilters() since tenants are resolved before query filters apply.
    /// </summary>
    public class TenantRepository : ITenantRepository
    {
        private readonly ApplicationDbContext _context;

        public TenantRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Tenant?> GetByIdentifierAsync(string identifier)
        {
            return await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Identifier == identifier);
        }

        public async Task<Tenant?> GetByIdAsync(Guid id)
        {
            return await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(Tenant tenant)
        {
            await _context.Tenants.AddAsync(tenant);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

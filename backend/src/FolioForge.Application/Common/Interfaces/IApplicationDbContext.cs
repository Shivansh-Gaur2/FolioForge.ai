using FolioForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Portfolio> Portfolios { get; }
    DbSet<PortfolioSection> Sections { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
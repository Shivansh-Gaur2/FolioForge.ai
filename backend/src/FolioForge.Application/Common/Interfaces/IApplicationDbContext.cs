using FolioForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Portfolio> Portfolios { get; }

    // We named this 'Sections' in your previous fix, so we match it here.
    DbSet<PortfolioSection> Sections { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
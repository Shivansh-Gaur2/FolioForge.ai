using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Infrastructure.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly ApplicationDbContext _context;

    public PlanRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Plan?> GetByIdAsync(Guid id)
    {
        return await _context.Plans.FindAsync(id);
    }

    public async Task<Plan?> GetBySlugAsync(string slug)
    {
        return await _context.Plans.FirstOrDefaultAsync(p => p.Slug == slug);
    }

    public async Task<List<Plan>> GetAllAsync()
    {
        return await _context.Plans.OrderBy(p => p.PriceMonthlyInCents).ToListAsync();
    }
}

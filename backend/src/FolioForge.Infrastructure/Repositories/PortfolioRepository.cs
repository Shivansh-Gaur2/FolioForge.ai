using FolioForge.Domain.Entities;
using FolioForge.Domain.Interfaces;
using FolioForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolioForge.Infrastructure.Repositories
{
    public class PortfolioRepository : IPortfolioRepository
    {
        private readonly ApplicationDbContext _context;

        public PortfolioRepository(ApplicationDbContext context)
        { 
            _context = context;
        }

        public async Task AddAsync(Portfolio portfolio)
        {
            await _context.Portfolios.AddAsync(portfolio);
        }

        public async Task<Portfolio?> GetByIdAsync(Guid id)
        {
            // Include sections when retrieving portfolio because of lazy loading disabled
            return await _context.Portfolios.Include(p => p.Sections).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Portfolio?> GetBySlugAsync(string slug)
        {
            return await _context.Portfolios.Include(p => p.Sections).FirstOrDefaultAsync(p => p.Slug == slug);
        }

        public Task SaveChangesAsync()
        {
            _context.SaveChanges();
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Portfolio portfolio)
        {
            _context.Portfolios.Update(portfolio);
            return Task.CompletedTask;
        }
    }
}

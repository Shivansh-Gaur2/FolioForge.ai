using FolioForge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolioForge.Domain.Interfaces
{
    public interface IPortfolioRepository
    {
        Task<Portfolio?> GetByIdAsync(Guid id);
        Task<Portfolio?> GetBySlugAsync(string slug);
        /// <summary>
        /// Retrieves a published portfolio by slug, bypassing tenant query filters.
        /// Used for public portfolio viewing.
        /// </summary>
        Task<Portfolio?> GetPublishedBySlugAsync(string slug);

        Task AddAsync(Portfolio portfolio);
        Task UpdateAsync(Portfolio portfolio);
        Task DeleteAsync(Portfolio portfolio);
        Task SaveChangesAsync();

    }
}

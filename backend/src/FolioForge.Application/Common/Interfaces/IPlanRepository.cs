using FolioForge.Domain.Entities;

namespace FolioForge.Application.Common.Interfaces;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid id);
    Task<Plan?> GetBySlugAsync(string slug);
    Task<List<Plan>> GetAllAsync();
}

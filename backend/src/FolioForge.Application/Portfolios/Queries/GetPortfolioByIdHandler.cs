using FolioForge.Application.Common;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Application.Portfolios.Queries.GetPortfolioById;

public class GetPortfolioByIdHandler : IRequestHandler<GetPortfolioByIdQuery, PortfolioDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public GetPortfolioByIdHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PortfolioDto?> Handle(GetPortfolioByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.PortfolioById(request.Id);

        // Try cache first (cache-aside pattern)
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            // 1. Fetch Portfolio + Sections from DB
            var entity = await _context.Portfolios
                .Include(p => p.Sections) // <--- CRITICAL: Use .Include() to get the sections!
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (entity == null) return null!;

            // 2. Map Entity to DTO (Data Transfer Object)
            return new PortfolioDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Slug = entity.Slug,
                Theme = new ThemeConfigDto
                {
                    Name = entity.Theme.Name,
                    PrimaryColor = entity.Theme.PrimaryColor,
                    SecondaryColor = entity.Theme.SecondaryColor,
                    BackgroundColor = entity.Theme.BackgroundColor,
                    TextColor = entity.Theme.TextColor,
                    FontHeading = entity.Theme.FontHeading,
                    FontBody = entity.Theme.FontBody,
                    Layout = entity.Theme.Layout
                },
                // Map the sections so React can see them
                Sections = entity.Sections
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new PortfolioSectionDto
                {
                    Id = s.Id,
                    SectionType = s.SectionType,
                    Content = s.Content,
                    SortOrder = s.SortOrder,
                    IsVisible = s.IsVisible,
                    Variant = s.Variant
                }).ToList()
            };
        }, CacheKeys.PortfolioTtl, cancellationToken);
    }
}
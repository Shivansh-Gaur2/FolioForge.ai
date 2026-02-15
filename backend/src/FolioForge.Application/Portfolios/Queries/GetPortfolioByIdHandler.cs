using FolioForge.Application.Common.Interfaces;
using FolioForge.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Application.Portfolios.Queries.GetPortfolioById;

public class GetPortfolioByIdHandler : IRequestHandler<GetPortfolioByIdQuery, PortfolioDto?>
{
    private readonly IApplicationDbContext _context;

    public GetPortfolioByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PortfolioDto?> Handle(GetPortfolioByIdQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch Portfolio + Sections from DB
        var entity = await _context.Portfolios
            .Include(p => p.Sections) // <--- CRITICAL: Use .Include() to get the sections!
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (entity == null) return null;

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
                FontBody = entity.Theme.FontBody
            },
            // Map the sections so React can see them
            Sections = entity.Sections.Select(s => new PortfolioSectionDto
            {
                Id = s.Id,
                SectionType = s.SectionType,
                Content = s.Content,
                SortOrder = s.SortOrder
            }).ToList()
        };
    }
}
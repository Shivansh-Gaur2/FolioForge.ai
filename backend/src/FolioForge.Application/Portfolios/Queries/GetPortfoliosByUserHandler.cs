using FolioForge.Application.Common.Interfaces;
using FolioForge.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Application.Portfolios.Queries;

public class GetPortfoliosByUserHandler : IRequestHandler<GetPortfoliosByUserQuery, List<PortfolioDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPortfoliosByUserHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PortfolioDto>> Handle(GetPortfoliosByUserQuery request, CancellationToken cancellationToken)
    {
        var portfolios = await _context.Portfolios
            .Where(p => p.UserId == request.UserId)
            .Include(p => p.Sections)
            .OrderByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

        return portfolios.Select(entity => new PortfolioDto
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
        }).ToList();
    }
}

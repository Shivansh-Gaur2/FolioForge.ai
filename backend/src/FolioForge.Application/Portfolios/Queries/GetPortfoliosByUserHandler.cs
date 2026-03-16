using FolioForge.Application.Common;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Application.Portfolios.Queries;

public class GetPortfoliosByUserHandler : IRequestHandler<GetPortfoliosByUserQuery, PagedResult<PortfolioDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public GetPortfoliosByUserHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PagedResult<PortfolioDto>> Handle(GetPortfoliosByUserQuery request, CancellationToken cancellationToken)
    {
        // Clamp page/pageSize to safe bounds
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var cacheKey = $"{CacheKeys.PortfoliosByUser(request.UserId)}:p{page}:s{pageSize}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var query = _context.Portfolios
                .Where(p => p.UserId == request.UserId)
                .OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var portfolios = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Sections)
                .ToListAsync(cancellationToken);

            var items = portfolios.Select(entity => new PortfolioDto
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

            return new PagedResult<PortfolioDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }, CacheKeys.UserPortfolioListTtl, cancellationToken) ?? new PagedResult<PortfolioDto>();
    }
}

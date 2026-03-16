using FolioForge.Application.Common;
using FolioForge.Application.DTOs;
using MediatR;

namespace FolioForge.Application.Portfolios.Queries;

/// <summary>
/// Returns paginated portfolios for the given user (within current tenant scope).
/// Defaults: page 1, pageSize 10, max 50.
/// </summary>
public record GetPortfoliosByUserQuery(Guid UserId, int Page = 1, int PageSize = 10)
    : IRequest<PagedResult<PortfolioDto>>;

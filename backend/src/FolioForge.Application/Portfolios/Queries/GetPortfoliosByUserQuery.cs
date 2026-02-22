using FolioForge.Application.DTOs;
using MediatR;

namespace FolioForge.Application.Portfolios.Queries;

/// <summary>
/// Returns all portfolios for the given user (within current tenant scope).
/// </summary>
public record GetPortfoliosByUserQuery(Guid UserId) : IRequest<List<PortfolioDto>>;

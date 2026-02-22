using MediatR;

namespace FolioForge.Application.Commands.DeletePortfolio;

/// <summary>
/// Command to delete a portfolio owned by the specified user.
/// Returns true if deleted, false if not found / not owned.
/// </summary>
public record DeletePortfolioCommand(Guid PortfolioId, Guid UserId) : IRequest<bool>;

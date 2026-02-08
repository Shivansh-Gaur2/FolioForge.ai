using FolioForge.Domain.Common;
using MediatR;

namespace FolioForge.Application.Commands.CreatePortfolio
{
    public record class CreatePortfolioCommand(Guid UserId, string Title, string DesiredSlug) : IRequest<Result<Guid>>;
}

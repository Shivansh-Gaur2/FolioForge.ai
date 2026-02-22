using FolioForge.Domain.Interfaces;
using MediatR;

namespace FolioForge.Application.Commands.DeletePortfolio;

public class DeletePortfolioCommandHandler : IRequestHandler<DeletePortfolioCommand, bool>
{
    private readonly IPortfolioRepository _repository;

    public DeletePortfolioCommandHandler(IPortfolioRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeletePortfolioCommand request, CancellationToken cancellationToken)
    {
        var portfolio = await _repository.GetByIdAsync(request.PortfolioId);

        // Not found or not owned by this user → 404
        if (portfolio is null || portfolio.UserId != request.UserId)
            return false;

        await _repository.DeleteAsync(portfolio);
        await _repository.SaveChangesAsync();

        return true;
    }
}

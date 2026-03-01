using FolioForge.Application.Common;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Interfaces;
using MediatR;

namespace FolioForge.Application.Commands.DeletePortfolio;

public class DeletePortfolioCommandHandler : IRequestHandler<DeletePortfolioCommand, bool>
{
    private readonly IPortfolioRepository _repository;
    private readonly ICacheService _cache;

    public DeletePortfolioCommandHandler(IPortfolioRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<bool> Handle(DeletePortfolioCommand request, CancellationToken cancellationToken)
    {
        var portfolio = await _repository.GetByIdAsync(request.PortfolioId);

        // Not found or not owned by this user → 404
        if (portfolio is null || portfolio.UserId != request.UserId)
            return false;

        await _repository.DeleteAsync(portfolio);
        await _repository.SaveChangesAsync();

        // Invalidate caches for this portfolio and user's list
        await _cache.RemoveAsync(CacheKeys.PortfolioById(request.PortfolioId), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.PortfoliosByUser(request.UserId), cancellationToken);

        return true;
    }
}

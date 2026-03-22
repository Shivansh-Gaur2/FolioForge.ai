using FolioForge.Application.Common;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Common;
using FolioForge.Domain.Entities;
using FolioForge.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolioForge.Application.Commands.CreatePortfolio
{
    public class CreatePortfolioCommandHandler : IRequestHandler<CreatePortfolioCommand, Result<Guid>>
    {
        private readonly IPortfolioRepository _repository;
        private readonly ITenantContext _tenantContext;
        private readonly ICacheService _cache;

        public CreatePortfolioCommandHandler(IPortfolioRepository repository, ITenantContext tenantContext, ICacheService cache)
        {
            _repository = repository;
            _tenantContext = tenantContext;
            _cache = cache;
        }

        public async Task<Result<Guid>> Handle(CreatePortfolioCommand request, CancellationToken ct = default)
        {
            var existing = await _repository.GetBySlugAsync(request.DesiredSlug);
            if(existing != null)
            {
                return Result<Guid>.Failure($"The URL '{request.DesiredSlug}' is already taken.");
            }

            var portfolio = new Portfolio(request.UserId, _tenantContext.TenantId, request.DesiredSlug, request.Title);

            var defaultBio = new { content = "Welcome to my portfolio! I am a software engineer..." };
            portfolio.AddSection(PortfolioSection.Create("About", 1, defaultBio));

            await _repository.AddAsync(portfolio);
            await _repository.SaveChangesAsync();

            // Invalidate user's portfolio list cache (prefix-based to clear all paginated variants)
            await _cache.RemoveByPrefixAsync(CacheKeys.PortfoliosByUser(request.UserId), ct);

            return Result<Guid>.Success(portfolio.Id);
        }
    }
}

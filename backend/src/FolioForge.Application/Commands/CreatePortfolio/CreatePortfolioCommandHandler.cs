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

        public CreatePortfolioCommandHandler(IPortfolioRepository repository, ITenantContext tenantContext)
        {
            _repository = repository;
            _tenantContext = tenantContext;
        }

        public async Task<Result<Guid>> Handle(CreatePortfolioCommand request, CancellationToken ct = default)
        {
            var existing = await _repository.GetBySlugAsync(request.DesiredSlug);
            if(existing != null)
            {
                return Result<Guid>.Failure($"The URL '{request.DesiredSlug}' is already taken.");
            }

            var portfolio = new Portfolio(request.UserId, _tenantContext.TenantId, request.DesiredSlug, request.Title);

            var defaultBio = new { text = "Welcome to my portfolio! I am a software engineer..." };
            portfolio.AddSection(PortfolioSection.Create("Markdown", 0, defaultBio));

            await _repository.AddAsync(portfolio);
            await _repository.SaveChangesAsync();

            return Result<Guid>.Success(portfolio.Id);
        }
    }
}

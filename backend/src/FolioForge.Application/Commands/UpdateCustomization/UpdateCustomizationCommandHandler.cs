using FolioForge.Application.Common;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Interfaces;
using MediatR;

namespace FolioForge.Application.Commands.UpdateCustomization;

public class UpdateCustomizationCommandHandler : IRequestHandler<UpdateCustomizationCommand, bool>
{
    private readonly IPortfolioRepository _repository;
    private readonly ICacheService _cache;

    public UpdateCustomizationCommandHandler(IPortfolioRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<bool> Handle(UpdateCustomizationCommand request, CancellationToken cancellationToken)
    {
        var portfolio = await _repository.GetByIdAsync(request.PortfolioId);

        // Not found or not owned by this user → 404
        if (portfolio is null || portfolio.UserId != request.UserId)
            return false;

        // 1. Update theme/colors/fonts/layout
        portfolio.UpdateCustomization(
            request.ThemeName,
            request.PrimaryColor,
            request.SecondaryColor,
            request.BackgroundColor,
            request.TextColor,
            request.FontHeading,
            request.FontBody,
            request.Layout
        );

        // 2. Update section order, visibility, variant, and optionally content
        foreach (var sectionUpdate in request.Sections)
        {
            var section = portfolio.Sections.FirstOrDefault(s => s.Id == sectionUpdate.SectionId);
            if (section is null) continue;

            section.SortOrder = sectionUpdate.SortOrder;
            section.IsVisible = sectionUpdate.IsVisible;
            section.Variant   = sectionUpdate.Variant;

            // Only update content when the client explicitly sends a new value
            if (sectionUpdate.Content is not null)
            {
                try
                {
                    section.UpdateContentRaw(sectionUpdate.Content);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    // Malformed JSON from client — skip silently rather than aborting the whole save
                    Console.Error.WriteLine($"[UpdateCustomization] Invalid JSON for section {sectionUpdate.SectionId}: {ex.Message}");
                }
            }
        }

        await _repository.UpdateAsync(portfolio);
        await _repository.SaveChangesAsync();

        // Invalidate caches — the portfolio and the user's list
        await _cache.RemoveAsync(CacheKeys.PortfolioById(request.PortfolioId), cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.PortfoliosByUser(request.UserId), cancellationToken);

        return true;
    }
}

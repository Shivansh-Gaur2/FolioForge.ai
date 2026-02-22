using FolioForge.Domain.Interfaces;
using MediatR;

namespace FolioForge.Application.Commands.UpdateCustomization;

public class UpdateCustomizationCommandHandler : IRequestHandler<UpdateCustomizationCommand, bool>
{
    private readonly IPortfolioRepository _repository;

    public UpdateCustomizationCommandHandler(IPortfolioRepository repository)
    {
        _repository = repository;
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

        // 2. Update section order, visibility, and variant
        foreach (var sectionUpdate in request.Sections)
        {
            var section = portfolio.Sections.FirstOrDefault(s => s.Id == sectionUpdate.SectionId);
            if (section is null) continue;

            section.SortOrder = sectionUpdate.SortOrder;
            section.IsVisible = sectionUpdate.IsVisible;
            section.Variant = sectionUpdate.Variant;
        }

        await _repository.UpdateAsync(portfolio);
        await _repository.SaveChangesAsync();

        return true;
    }
}

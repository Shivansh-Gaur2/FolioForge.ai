using MediatR;

namespace FolioForge.Application.Commands.UpdateCustomization;

/// <summary>
/// Command to update the full customization (theme + section layout) of a portfolio.
/// </summary>
public record UpdateCustomizationCommand(
    Guid PortfolioId,
    Guid UserId,
    // Theme / colors
    string ThemeName,
    string PrimaryColor,
    string SecondaryColor,
    string BackgroundColor,
    string TextColor,
    // Typography
    string FontHeading,
    string FontBody,
    // Layout
    string Layout,
    // Sections customization (order, visibility, variant)
    List<SectionCustomization> Sections
) : IRequest<bool>;

/// <summary>
/// Lightweight DTO for section-level customization (no content change).
/// </summary>
public record SectionCustomization(
    Guid SectionId,
    int SortOrder,
    bool IsVisible,
    string Variant
);

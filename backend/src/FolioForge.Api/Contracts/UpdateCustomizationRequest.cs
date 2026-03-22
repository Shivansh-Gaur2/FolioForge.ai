using System.ComponentModel.DataAnnotations;

namespace FolioForge.Api.Contracts;

public class UpdateCustomizationRequest
{
    [Required, StringLength(50)]
    public string ThemeName { get; set; } = "default";

    [Required, RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Must be a valid hex color, e.g. '#3B82F6'.")]
    public string PrimaryColor { get; set; } = "#3B82F6";

    [Required, RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Must be a valid hex color.")]
    public string SecondaryColor { get; set; } = "#10B981";

    [Required, RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Must be a valid hex color.")]
    public string BackgroundColor { get; set; } = "#FFFFFF";

    [Required, RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Must be a valid hex color.")]
    public string TextColor { get; set; } = "#1F2937";

    [Required, StringLength(50)]
    public string FontHeading { get; set; } = "Inter";

    [Required, StringLength(50)]
    public string FontBody { get; set; } = "Inter";

    [Required, RegularExpression(@"^(single-column|two-column|sidebar)$", ErrorMessage = "Layout must be 'single-column', 'two-column', or 'sidebar'.")]
    public string Layout { get; set; } = "single-column";

    public List<SectionCustomizationItem> Sections { get; set; } = new();
}

public class SectionCustomizationItem
{
    public Guid SectionId { get; set; }

    [Range(0, 100)]
    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    [Required, StringLength(50)]
    public string Variant { get; set; } = "default";

    /// <summary>
    /// Optional raw JSON content for the section.
    /// When present the section body is replaced with this value.
    /// Null means "leave content unchanged".
    /// </summary>
    public string? Content { get; set; }
}

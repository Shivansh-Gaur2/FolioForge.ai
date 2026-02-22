namespace FolioForge.Api.Contracts;

public class UpdateCustomizationRequest
{
    public string ThemeName { get; set; } = "default";
    public string PrimaryColor { get; set; } = "#3B82F6";
    public string SecondaryColor { get; set; } = "#10B981";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string TextColor { get; set; } = "#1F2937";
    public string FontHeading { get; set; } = "Inter";
    public string FontBody { get; set; } = "Inter";
    public string Layout { get; set; } = "single-column";
    public List<SectionCustomizationItem> Sections { get; set; } = new();
}

public class SectionCustomizationItem
{
    public Guid SectionId { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public string Variant { get; set; } = "default";
}

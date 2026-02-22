using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolioForge.Application.DTOs
{
    public class ThemeConfigDto
    {
        public string Name { get; set; } = "default";
        public string PrimaryColor { get; set; } = "#3B82F6";
        public string SecondaryColor { get; set; } = "#10B981";
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public string TextColor { get; set; } = "#1F2937";
        public string FontHeading { get; set; } = "Inter";
        public string FontBody { get; set; } = "Inter";
        public string Layout { get; set; } = "single-column";
    }

    public class PortfolioDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public ThemeConfigDto Theme { get; set; } = new();
        public bool IsPublished { get; set; }
        public List<PortfolioSectionDto> Sections { get; set; } = new();
    }
}

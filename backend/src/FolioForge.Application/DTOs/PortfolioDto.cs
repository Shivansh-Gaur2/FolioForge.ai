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
        public string PrimaryColor { get; set; } = "#000000";
        public string FontBody { get; set; } = "Inter";
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

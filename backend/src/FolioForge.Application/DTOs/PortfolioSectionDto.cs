using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolioForge.Application.DTOs
{
    public class PortfolioSectionDto
    {
        public Guid Id { get; set; }
        public string SectionType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}

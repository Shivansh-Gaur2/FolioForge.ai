using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FolioForge.Domain.Interfaces;

namespace FolioForge.Domain.Entities
{
    public class Portfolio : BaseEntity, ITenantEntity
    {
        // private set because we don't want to allow external code to modify these properties directly,
        // we want to control how they are set through methods in the class.

        // The slug is unique identifier for the portfolio, it will be used in the URL to access the portfolio, so it needs to be unique and not changeable after it's set.
        public string Slug { get; private set; } = default!;

        public Guid UserId { get; private set; }
        public Guid TenantId { get; set; }
        public string Title { get; private set; } = default!;
        public bool IsPublished { get; private set; }


        public List<PortfolioSection> Sections { get; private set; } = new();

        // record because i want to keep it read only 
        // i will use it more like a DTO because I will need something for the 
        // info to like theme to be set and send to the frontend 
        public record ThemeConfig(string Name, string PrimaryColor, string FontBody);
        public ThemeConfig Theme { get; private set; } = default!;

        private Portfolio() { }

        public Portfolio(Guid userId, Guid tenantId, string slug, string title)
        {
            UserId = userId;
            TenantId = tenantId;
            Slug = slug;
            Title = title;
            IsPublished = true;
            Theme = new ThemeConfig("default", "#000000", "Inter");
        }

        public void AddSection(PortfolioSection section)
        {
            Sections.Add(section);
        }

        public void UpdateTheme(string primaryColor, string font)
        {
            Theme = new ThemeConfig(Theme.Name, primaryColor, font);
        }
    }
}

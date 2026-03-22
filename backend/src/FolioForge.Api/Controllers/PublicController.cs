using FolioForge.Application.Common.Interfaces;
using FolioForge.Application.DTOs;
using FolioForge.Domain.Interfaces;
using FolioForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Api.Controllers
{
    /// <summary>
    /// Public endpoints that do NOT require authentication.
    /// Used for viewing published portfolios (e.g. shared links on LinkedIn).
    /// </summary>
    [ApiController]
    [Route("api/p")]
    public class PublicController : ControllerBase
    {
        private readonly IPortfolioRepository _repository;
        private readonly ApplicationDbContext _dbContext;
        private readonly IPlanRepository _planRepository;

        public PublicController(
            IPortfolioRepository repository,
            ApplicationDbContext dbContext,
            IPlanRepository planRepository)
        {
            _repository = repository;
            _dbContext = dbContext;
            _planRepository = planRepository;
        }

        /// <summary>
        /// View a published portfolio by its slug.
        /// GET /api/p/{slug}
        /// No authentication required — this is the public-facing URL.
        /// </summary>
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetPublishedPortfolio(string slug)
        {
            var portfolio = await _repository.GetPublishedBySlugAsync(slug);
            if (portfolio == null)
                return NotFound(new { error = "Portfolio not found or is not published." });

            // Determine if watermark should be shown based on portfolio owner's plan
            var owner = await _dbContext.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == portfolio.UserId);
            var plan = owner != null ? await _planRepository.GetByIdAsync(owner.PlanId) : null;
            var showWatermark = plan == null || !plan.RemoveWatermark;

            return Ok(new
            {
                id = portfolio.Id,
                title = portfolio.Title,
                slug = portfolio.Slug,
                isPublished = portfolio.IsPublished,
                showWatermark,
                theme = new ThemeConfigDto
                {
                    Name = portfolio.Theme.Name,
                    PrimaryColor = portfolio.Theme.PrimaryColor,
                    SecondaryColor = portfolio.Theme.SecondaryColor,
                    BackgroundColor = portfolio.Theme.BackgroundColor,
                    TextColor = portfolio.Theme.TextColor,
                    FontHeading = portfolio.Theme.FontHeading,
                    FontBody = portfolio.Theme.FontBody,
                    Layout = portfolio.Theme.Layout,
                },
                Sections = portfolio.Sections
                    .Where(s => s.IsVisible)
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new PortfolioSectionDto
                    {
                        Id = s.Id,
                        SectionType = s.SectionType,
                        Content = s.Content,
                        SortOrder = s.SortOrder,
                        IsVisible = s.IsVisible,
                        Variant = s.Variant ?? "default",
                    })
                    .ToList(),
            });
        }
    }
}

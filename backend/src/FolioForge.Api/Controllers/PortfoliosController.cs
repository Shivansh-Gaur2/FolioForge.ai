using FolioForge.Api.Contracts;
using FolioForge.Application.Commands.CreatePortfolio;
using FolioForge.Application.Commands.DeletePortfolio;
using FolioForge.Application.Commands.UpdateCustomization;
using FolioForge.Application.Common.Events;
using FolioForge.Application.Portfolios.Queries;
using FolioForge.Domain.Interfaces;
using FolioForge.Infrastructure.RateLimiting;
using FolioForge.Infrastructure.Resilience.Bulkhead;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace FolioForge.Api.Controllers
{
    /// <summary>
    /// Portfolio CRUD endpoints.
    /// Uses "Default" policy (20 burst, 10/s sustained) for most operations.
    /// Upload endpoint overrides with "Upload" policy (stricter).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PortfoliosController : ControllerBase
    {
        private readonly ISender _mediator;
        private readonly IEventPublisher _publisher;

        public PortfoliosController(ISender mediator, IEventPublisher publisher)
        {
            _mediator = mediator;
            _publisher = publisher;
        }

        /// <summary>
        /// Helper to extract UserId from the JWT claims.
        /// </summary>
        private Guid GetUserId()
        {
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(sub!);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePortfolioRequest request)
        {
            var userId = GetUserId();

            var command = new CreatePortfolioCommand(userId, request.Title, request.Slug);

            var result = await _mediator.Send(command);

            if (!result.IsSuccess) return BadRequest(new { error = result.Error });

            return CreatedAtAction(
                nameof(GetBySlug),
                new { slug = request.Slug },
                new { id = result.Value }
             );

        }

        /// <summary>
        /// List portfolios for the current authenticated user with pagination.
        /// GET /api/portfolios/mine?page=1&amp;pageSize=10
        /// </summary>
        [HttpGet("mine")]
        public async Task<IActionResult> ListMine([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            var query = new GetPortfoliosByUserQuery(userId, page, pageSize);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetPortfolioByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result == null) return NotFound();

            return Ok(result); // Returns the actual JSON data!
        }

        [HttpGet("{slug}")]
        public IActionResult GetBySlug(string slug)
        {
            return Ok(new { message = $"Fetching portfolio: {slug}" });
        }

        /// <summary>
        /// Delete a portfolio owned by the current user.
        /// DELETE /api/portfolios/{id}
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            var command = new DeletePortfolioCommand(id, userId);
            var result = await _mediator.Send(command);

            if (!result) return NotFound();

            return NoContent();
        }

        private static readonly string[] AllowedExtensions = { ".pdf" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        [HttpPost("{id}/upload-resume")]
        [RateLimit("Upload")]
        [Bulkhead("Upload")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB hard limit at Kestrel level
        public async Task<IActionResult> UploadResume(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            if (file.Length > MaxFileSizeBytes)
                return BadRequest(new { error = $"File size exceeds the {MaxFileSizeBytes / (1024 * 1024)} MB limit." });

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                return BadRequest(new { error = "Only PDF files are accepted." });

            // Verify the file starts with %PDF magic bytes (not just the extension)
            using var headerStream = file.OpenReadStream();
            var header = new byte[5];
            var bytesRead = await headerStream.ReadAsync(header, 0, header.Length);
            if (bytesRead < 5 || System.Text.Encoding.ASCII.GetString(header) != "%PDF-")
                return BadRequest(new { error = "File content is not a valid PDF." });

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, $"{id}_{Guid.NewGuid()}.pdf");
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await _publisher.PublishAsync(new ResumeUploadedEvent(id, filePath));

            // Return 202 Accepted — tells the frontend: "We got it. We are working on it."
            return Accepted(new { message = "Resume queued for processing", portfolioId = id });
        }

        /// <summary>
        /// Update the full customization (theme, colors, fonts, layout, section order/visibility/variant).
        /// PUT /api/portfolios/{id}/customization
        /// </summary>
        [HttpPut("{id:guid}/customization")]
        public async Task<IActionResult> UpdateCustomization(Guid id, [FromBody] UpdateCustomizationRequest request)
        {
            var userId = GetUserId();

            var command = new UpdateCustomizationCommand(
                id, userId,
                request.ThemeName,
                request.PrimaryColor,
                request.SecondaryColor,
                request.BackgroundColor,
                request.TextColor,
                request.FontHeading,
                request.FontBody,
                request.Layout,
                request.Sections.Select(s => new SectionCustomization(
                    s.SectionId, s.SortOrder, s.IsVisible, s.Variant, s.Content
                )).ToList()
            );

            var result = await _mediator.Send(command);
            if (!result) return NotFound();

            return NoContent();
        }
    }
}

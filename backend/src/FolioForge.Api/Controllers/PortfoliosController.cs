using FolioForge.Api.Contracts;
using FolioForge.Application.Commands.CreatePortfolio;
using FolioForge.Application.Common.Events;
using FolioForge.Application.Portfolios.Queries;
using FolioForge.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace FolioForge.Api.Controllers
{
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

        [HttpPost("{id}/upload-resume")]
        public async Task<IActionResult> UploadResume(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if(!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, $"{id}_{Guid.NewGuid()}.pdf");
            using ( var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await _publisher.PublishAsync(new ResumeUploadedEvent(id, filePath));

            // 3. Return 202 Accepted
            // This tells the frontend: "We got it. We are working on it."
            return Accepted(new { message = "Resume queued for processing", portfolioId = id });
        }
    }
}

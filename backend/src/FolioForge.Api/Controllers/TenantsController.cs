using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FolioForge.Api.Controllers
{
    /// <summary>
    /// Controller for tenant management operations.
    /// These endpoints are EXCLUDED from tenant middleware (no X-Tenant-Id required).
    /// Rate limited with "Strict" policy to prevent tenant enumeration.
    /// Tenant creation requires authentication; only authorized users can provision tenants.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [RateLimit("Strict")]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantRepository _tenantRepository;

        public TenantsController(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        /// <summary>
        /// Register a new tenant.
        /// POST /api/tenants { "name": "Acme Corp", "identifier": "acme-corp" }
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
        {
            var existing = await _tenantRepository.GetByIdentifierAsync(request.Identifier);
            if (existing != null)
                return Conflict(new { error = $"Tenant '{request.Identifier}' already exists." });

            var tenant = new Tenant(request.Name, request.Identifier);
            await _tenantRepository.AddAsync(tenant);
            await _tenantRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, new
            {
                id = tenant.Id,
                name = tenant.Name,
                identifier = tenant.Identifier
            });
        }

        /// <summary>
        /// Get tenant by ID.
        /// GET /api/tenants/{id}
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null) return NotFound();

            return Ok(new
            {
                id = tenant.Id,
                name = tenant.Name,
                identifier = tenant.Identifier,
                isActive = tenant.IsActive
            });
        }
    }

    public record CreateTenantRequest(
        [Required, StringLength(100, MinimumLength = 2)] string Name,
        [Required, StringLength(50, MinimumLength = 3), RegularExpression(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$",
            ErrorMessage = "Identifier must be lowercase alphanumeric with hyphens, e.g. 'my-company'.")]
        string Identifier
    );
}

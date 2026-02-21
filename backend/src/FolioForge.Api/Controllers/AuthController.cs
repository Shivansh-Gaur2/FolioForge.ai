using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FolioForge.Api.Controllers
{
    /// <summary>
    /// Authentication endpoints.
    /// Register and Login are EXCLUDED from tenant middleware
    /// because the tenant is resolved from the request body (register) 
    /// or from the user's stored tenantId (login).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IAuthService _authService;

        public AuthController(
            IUserRepository userRepository,
            ITenantRepository tenantRepository,
            IAuthService authService)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _authService = authService;
        }

        /// <summary>
        /// Register a new user under a specific tenant.
        /// POST /api/auth/register
        /// {
        ///   "email": "user@example.com",
        ///   "fullName": "Shivansh Dev",
        ///   "password": "MySecurePass123",
        ///   "tenantIdentifier": "test-tenant"
        /// }
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // 1. Validate tenant exists
            var tenant = await _tenantRepository.GetByIdentifierAsync(request.TenantIdentifier);
            if (tenant == null)
                return BadRequest(new { error = $"Tenant '{request.TenantIdentifier}' not found." });

            if (!tenant.IsActive)
                return BadRequest(new { error = "Tenant is deactivated." });

            // 2. Check if email is already used (globally)
            if (await _userRepository.EmailExistsGloballyAsync(request.Email))
                return Conflict(new { error = "An account with this email already exists." });

            // 3. Hash password and create user
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User(request.Email, request.FullName, passwordHash, tenant.Id);

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // 4. Generate JWT
            var token = _authService.GenerateToken(user.Id, tenant.Id, user.Email, user.FullName);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                TenantId = tenant.Id,
                TenantIdentifier = tenant.Identifier
            });
        }

        /// <summary>
        /// Login with email and password.
        /// POST /api/auth/login
        /// {
        ///   "email": "user@example.com",
        ///   "password": "MySecurePass123"
        /// }
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Look up user across all tenants (bypasses query filters)
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(new { error = "Invalid email or password." });

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { error = "Invalid email or password." });

            // Get the tenant to include identifier in response
            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null || !tenant.IsActive)
                return Unauthorized(new { error = "Your tenant is no longer active." });

            // Generate JWT
            var token = _authService.GenerateToken(user.Id, tenant.Id, user.Email, user.FullName);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                TenantId = tenant.Id,
                TenantIdentifier = tenant.Identifier
            });
        }

        /// <summary>
        /// Get current user profile from JWT claims.
        /// GET /api/auth/me
        /// Requires Authorization: Bearer {token}
        /// </summary>
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult Me()
        {
            var userId = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                      ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value
                     ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var fullName = User.FindFirst("fullName")?.Value;
            var tenantId = User.FindFirst("tenantId")?.Value;

            return Ok(new
            {
                userId,
                email,
                fullName,
                tenantId
            });
        }
    }

    // ── Request / Response DTOs ──
    public record RegisterRequest(
        string Email,
        string FullName,
        string Password,
        string TenantIdentifier
    );

    public record LoginRequest(
        string Email,
        string Password
    );

    public class AuthResponse
    {
        public string Token { get; set; } = default!;
        public Guid UserId { get; set; }
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public Guid TenantId { get; set; }
        public string TenantIdentifier { get; set; } = default!;
    }
}

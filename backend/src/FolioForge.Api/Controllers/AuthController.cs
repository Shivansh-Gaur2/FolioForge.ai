using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Infrastructure.Persistence;
using FolioForge.Infrastructure.RateLimiting;
using FolioForge.Infrastructure.Resilience.Bulkhead;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FolioForge.Api.Controllers
{
    /// <summary>
    /// Authentication endpoints with short-lived access tokens + refresh token rotation.
    /// 
    /// Flow:
    /// 1. Register/Login → returns { accessToken, refreshToken, expiresAt, ... }
    /// 2. Client stores refreshToken securely (HttpOnly cookie or secure storage).
    /// 3. When accessToken expires, call POST /api/auth/refresh with both tokens.
    /// 4. Server validates refresh token, rotates it, returns a new pair.
    /// 
    /// Rate limited with the "Auth" policy (strict: 5 burst, 2/s sustained)
    /// to prevent credential stuffing and brute-force attacks.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [RateLimit("Auth")]
    [Bulkhead("Auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthController(
            IUserRepository userRepository,
            ITenantRepository tenantRepository,
            IAuthService authService,
            ApplicationDbContext dbContext,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _authService = authService;
            _dbContext = dbContext;
            _configuration = configuration;
        }

        private int RefreshTokenExpirationDays =>
            int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        /// <summary>
        /// Register a new user under a specific tenant.
        /// Returns short-lived access token + long-lived refresh token.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var tenant = await _tenantRepository.GetByIdentifierAsync(request.TenantIdentifier);
            if (tenant == null)
                return BadRequest(new { error = $"Tenant '{request.TenantIdentifier}' not found." });

            if (!tenant.IsActive)
                return BadRequest(new { error = "Tenant is deactivated." });

            if (await _userRepository.EmailExistsGloballyAsync(request.Email))
                return Conflict(new { error = "An account with this email already exists." });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User(request.Email, request.FullName, passwordHash, tenant.Id);

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            var (accessToken, refreshToken) = await CreateTokenPairAsync(user, tenant);

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                TenantId = tenant.Id,
                TenantIdentifier = tenant.Identifier
            });
        }

        /// <summary>
        /// Login with email and password.
        /// Returns short-lived access token + long-lived refresh token.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(new { error = "Invalid email or password." });

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { error = "Invalid email or password." });

            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null || !tenant.IsActive)
                return Unauthorized(new { error = "Your tenant is no longer active." });

            var (accessToken, refreshToken) = await CreateTokenPairAsync(user, tenant);

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                TenantId = tenant.Id,
                TenantIdentifier = tenant.Identifier
            });
        }

        /// <summary>
        /// Refresh an expired access token using a valid refresh token.
        /// Implements token rotation: the old refresh token is revoked and a new one is issued.
        /// POST /api/auth/refresh { "accessToken": "...", "refreshToken": "..." }
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            // 1. Validate the expired access token's signature and extract claims
            var principal = _authService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
                return Unauthorized(new { error = "Invalid access token." });

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { error = "Invalid access token claims." });

            // 2. Find the refresh token in the database
            var storedToken = await _dbContext.RefreshTokens
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId);

            if (storedToken == null || !storedToken.IsActive)
            {
                // If someone tries to reuse a revoked token, revoke ALL tokens for this user
                // (potential token theft detected)
                if (storedToken?.RevokedAt != null)
                {
                    await RevokeAllUserTokensAsync(userId);
                }
                return Unauthorized(new { error = "Invalid or expired refresh token." });
            }

            // 3. Look up user and tenant
            var user = await _userRepository.GetByEmailAsync(
                principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? principal.FindFirst(ClaimTypes.Email)?.Value ?? "");
            if (user == null)
                return Unauthorized(new { error = "User not found." });

            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null || !tenant.IsActive)
                return Unauthorized(new { error = "Tenant is no longer active." });

            // 4. Rotate: revoke old, create new pair
            var newRefreshTokenString = _authService.GenerateRefreshTokenString();
            storedToken.Revoke(replacedByToken: newRefreshTokenString);

            var newRefreshToken = new RefreshToken(
                newRefreshTokenString,
                userId,
                DateTime.UtcNow.AddDays(RefreshTokenExpirationDays));

            await _dbContext.RefreshTokens.AddAsync(newRefreshToken);
            await _dbContext.SaveChangesAsync();

            var newAccessToken = _authService.GenerateAccessToken(
                user.Id, tenant.Id, user.Email, user.FullName);

            return Ok(new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenString,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                TenantId = tenant.Id,
                TenantIdentifier = tenant.Identifier
            });
        }

        /// <summary>
        /// Revoke a refresh token (logout).
        /// POST /api/auth/revoke { "refreshToken": "..." }
        /// </summary>
        [HttpPost("revoke")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request)
        {
            var token = await _dbContext.RefreshTokens
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (token == null || !token.IsActive)
                return BadRequest(new { error = "Token not found or already revoked." });

            token.Revoke();
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Token revoked." });
        }

        /// <summary>
        /// Get current user profile from JWT claims.
        /// </summary>
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult Me()
        {
            var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                     ?? User.FindFirst(ClaimTypes.Email)?.Value;
            var fullName = User.FindFirst("fullName")?.Value;
            var tenantId = User.FindFirst("tenantId")?.Value;

            return Ok(new { userId, email, fullName, tenantId });
        }

        // ─────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────

        private async Task<(string accessToken, string refreshToken)> CreateTokenPairAsync(User user, Tenant tenant)
        {
            var accessToken = _authService.GenerateAccessToken(user.Id, tenant.Id, user.Email, user.FullName);
            var refreshTokenString = _authService.GenerateRefreshTokenString();

            var refreshToken = new RefreshToken(
                refreshTokenString,
                user.Id,
                DateTime.UtcNow.AddDays(RefreshTokenExpirationDays));

            await _dbContext.RefreshTokens.AddAsync(refreshToken);
            await _dbContext.SaveChangesAsync();

            return (accessToken, refreshTokenString);
        }

        private async Task RevokeAllUserTokensAsync(Guid userId)
        {
            var activeTokens = await _dbContext.RefreshTokens
                .IgnoreQueryFilters()
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in activeTokens)
                token.Revoke();

            await _dbContext.SaveChangesAsync();
        }
    }

    // ── Request / Response DTOs ──
    public record RegisterRequest(
        [Required, EmailAddress, StringLength(254)] string Email,
        [Required, StringLength(100, MinimumLength = 2)] string FullName,
        [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")] string Password,
        [Required, StringLength(50)] string TenantIdentifier
    );

    public record LoginRequest(
        [Required, EmailAddress] string Email,
        [Required] string Password
    );

    public record RefreshTokenRequest(
        [Required] string AccessToken,
        [Required] string RefreshToken
    );

    public record RevokeTokenRequest(
        [Required] string RefreshToken
    );

    public class AuthResponse
    {
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
        public Guid UserId { get; set; }
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public Guid TenantId { get; set; }
        public string TenantIdentifier { get; set; } = default!;
    }
}

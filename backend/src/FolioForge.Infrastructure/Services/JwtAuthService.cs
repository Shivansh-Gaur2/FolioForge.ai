using FolioForge.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FolioForge.Infrastructure.Services
{
    /// <summary>
    /// JWT token generation service.
    /// Embeds userId, tenantId, email, and name in the token claims.
    /// The tenant middleware can then resolve the tenant from the JWT
    /// so authenticated users don't need to send X-Tenant-Id manually.
    /// </summary>
    public class JwtAuthService : IAuthService
    {
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;

        public JwtAuthService(IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("Jwt");
            _secret = jwtSection["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");
            _issuer = jwtSection["Issuer"] ?? "FolioForge";
            _audience = jwtSection["Audience"] ?? "FolioForge.Client";
            _expirationMinutes = int.Parse(jwtSection["ExpirationMinutes"] ?? "1440"); // Default: 24 hours
        }

        public string GenerateToken(Guid userId, Guid tenantId, string email, string fullName)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("fullName", fullName),
                new Claim("tenantId", tenantId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

using FolioForge.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FolioForge.Infrastructure.Services
{
    /// <summary>
    /// JWT access token generation + refresh token support.
    /// 
    /// Access tokens: short-lived (default 15 min), stateless JWT.
    /// Refresh tokens: opaque random strings, stored server-side.
    /// </summary>
    public class JwtAuthService : IAuthService
    {
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpirationMinutes;

        public JwtAuthService(IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("Jwt");
            _secret = jwtSection["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");
            _issuer = jwtSection["Issuer"] ?? "FolioForge";
            _audience = jwtSection["Audience"] ?? "FolioForge.Client";
            _accessTokenExpirationMinutes = int.Parse(
                jwtSection["AccessTokenExpirationMinutes"] ?? jwtSection["ExpirationMinutes"] ?? "15");
        }

        public string GenerateAccessToken(Guid userId, Guid tenantId, string email, string fullName)
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
                expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>Backward compat — delegates to GenerateAccessToken.</summary>
        public string GenerateToken(Guid userId, Guid tenantId, string email, string fullName)
            => GenerateAccessToken(userId, tenantId, email, fullName);

        public string GenerateRefreshTokenString()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Validates the structure and signature of an expired access token
        /// and extracts its claims. Lifetime validation is disabled so we can
        /// read claims from an expired token during refresh.
        /// </summary>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
                // Allow expired tokens — we only need to verify signature+issuer
                ValidateLifetime = false
            };

            try
            {
                var principal = new JwtSecurityTokenHandler()
                    .ValidateToken(accessToken, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}

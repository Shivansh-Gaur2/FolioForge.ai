using System.Security.Claims;

namespace FolioForge.Application.Common.Interfaces
{
    /// <summary>
    /// JWT token generation and refresh token management.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Generates a short-lived JWT access token containing userId, tenantId, and email claims.
        /// </summary>
        string GenerateAccessToken(Guid userId, Guid tenantId, string email, string fullName);

        /// <summary>
        /// Generates a cryptographically random opaque refresh token string.
        /// </summary>
        string GenerateRefreshTokenString();

        /// <summary>
        /// Validates an expired access token and extracts its claims (used during token refresh).
        /// Returns null if the token is structurally invalid.
        /// </summary>
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken);

        // Backward compat — delegates to GenerateAccessToken
        string GenerateToken(Guid userId, Guid tenantId, string email, string fullName)
            => GenerateAccessToken(userId, tenantId, email, fullName);
    }
}

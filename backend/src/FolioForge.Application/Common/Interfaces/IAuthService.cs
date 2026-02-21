namespace FolioForge.Application.Common.Interfaces
{
    /// <summary>
    /// JWT token generation service.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Generates a JWT containing userId, tenantId, and email claims.
        /// </summary>
        string GenerateToken(Guid userId, Guid tenantId, string email, string fullName);
    }
}

namespace FolioForge.Domain.Entities;

/// <summary>
/// Represents an opaque refresh token stored in the database.
/// 
/// Design:
/// ───────
/// - Access tokens are short-lived (15 min) and stateless (JWT).
/// - Refresh tokens are long-lived (7 days), opaque, and stored server-side.
/// - On each refresh, the old token is revoked and a new pair is issued (rotation).
/// - Token rotation means a stolen refresh token can only be used once — 
///   when the legitimate user tries to refresh next, the mismatch is detected
///   and ALL tokens for that user can be revoked.
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>The opaque token string (cryptographically random).</summary>
    public string Token { get; private set; } = default!;

    /// <summary>When this refresh token expires.</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>When this token was revoked (null if still active).</summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>The token that replaced this one (for rotation tracking).</summary>
    public string? ReplacedByToken { get; private set; }

    /// <summary>The user this token belongs to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Whether this token is still usable.</summary>
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    private RefreshToken() { } // EF Core

    public RefreshToken(string token, Guid userId, DateTime expiresAt)
    {
        Token = token;
        UserId = userId;
        ExpiresAt = expiresAt;
    }

    /// <summary>Revoke this token and record the replacement.</summary>
    public void Revoke(string? replacedByToken = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedByToken;
    }
}

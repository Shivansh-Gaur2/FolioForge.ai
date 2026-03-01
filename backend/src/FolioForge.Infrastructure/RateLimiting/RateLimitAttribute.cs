namespace FolioForge.Infrastructure.RateLimiting;

/// <summary>
/// Attribute to assign a named rate-limit policy to a controller or action.
///
/// Usage:
///   [RateLimit("Auth")]          → applies the "Auth" policy
///   [RateLimit("Upload")]        → applies the "Upload" policy
///   [RateLimit(Disabled = true)] → exempts this endpoint from rate limiting
///
/// When applied to both a controller and an action, the action-level attribute wins.
/// If no attribute is present, the middleware falls back to the "Default" policy.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RateLimitAttribute : Attribute
{
    /// <summary>Name of the rate-limit policy to apply.</summary>
    public string PolicyName { get; }

    /// <summary>Set to true to disable rate limiting for this endpoint.</summary>
    public bool Disabled { get; set; }

    public RateLimitAttribute(string policyName = "Default")
    {
        PolicyName = policyName;
    }
}

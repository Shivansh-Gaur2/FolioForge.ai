namespace FolioForge.Infrastructure.Resilience.CircuitBreaker;

/// <summary>
/// Configuration options for circuit breakers.
/// Bound from appsettings.json section "CircuitBreaker".
/// </summary>
public sealed class CircuitBreakerOptions
{
    public const string SectionName = "CircuitBreaker";

    /// <summary>Master switch to enable/disable circuit breakers globally.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Named circuit breaker configurations — one per downstream dependency.
    /// Keys are breaker names (e.g., "GroqAi", "RabbitMq"), values are parameters.
    /// </summary>
    public Dictionary<string, BreakerConfig> Breakers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Configuration for a single circuit breaker instance.
///
/// Tuning guidance:
/// ────────────────
/// • FailureThreshold: Higher values tolerate more transient failures before tripping.
///   For flaky external APIs, use 3-5. For critical internal services, use 2-3.
///
/// • OpenDurationSeconds: How long to wait before probing. Too short = hammering
///   a recovering service. Too long = unnecessarily degraded experience.
///   External APIs: 30-60s. Internal services: 10-15s.
///
/// • HalfOpenMaxAttempts: Number of probe requests in HalfOpen state.
///   1 = conservative (single success closes circuit).
///   3 = more confidence before closing.
///
/// • SuccessThresholdInHalfOpen: How many successful probes needed to close.
///   Must be <= HalfOpenMaxAttempts.
/// </summary>
public sealed class BreakerConfig
{
    /// <summary>Consecutive failures before opening the circuit.</summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>How long the circuit stays open before transitioning to half-open (seconds).</summary>
    public double OpenDurationSeconds { get; set; } = 30;

    /// <summary>Maximum number of probe requests allowed in half-open state.</summary>
    public int HalfOpenMaxAttempts { get; set; } = 2;

    /// <summary>Number of successful probes needed to close the circuit.</summary>
    public int SuccessThresholdInHalfOpen { get; set; } = 2;

    /// <summary>
    /// Optional: Types of exceptions that should be treated as failures.
    /// If empty, ALL exceptions trip the circuit.
    /// Example: ["System.Net.Http.HttpRequestException", "System.TimeoutException"]
    /// </summary>
    public List<string> HandledExceptionTypes { get; set; } = [];
}

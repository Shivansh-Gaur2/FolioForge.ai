namespace FolioForge.Infrastructure.Resilience.CircuitBreaker;

/// <summary>
/// Exception thrown when a circuit breaker is in the Open state.
/// Callers should catch this to provide graceful degradation (e.g., return cached data,
/// a default response, or a user-friendly error message).
/// </summary>
public sealed class CircuitBreakerOpenException : Exception
{
    /// <summary>Name of the circuit breaker that is open.</summary>
    public string BreakerName { get; }

    /// <summary>When the circuit will transition to half-open for probing.</summary>
    public DateTimeOffset RetryAfter { get; }

    public CircuitBreakerOpenException(string breakerName, DateTimeOffset retryAfter)
        : base($"Circuit breaker '{breakerName}' is open. Retry after {retryAfter:O}.")
    {
        BreakerName = breakerName;
        RetryAfter = retryAfter;
    }

    public CircuitBreakerOpenException(string breakerName, DateTimeOffset retryAfter, string message)
        : base(message)
    {
        BreakerName = breakerName;
        RetryAfter = retryAfter;
    }
}

using FolioForge.Application.Common.Interfaces;
using FolioForge.Infrastructure.Resilience.CircuitBreaker;
using Microsoft.Extensions.Logging;

namespace FolioForge.Infrastructure.Services;

/// <summary>
/// Decorator that wraps <see cref="IAiService"/> with Circuit Breaker protection.
///
/// When the Groq AI API starts failing (timeouts, 5xx errors), the circuit opens
/// and subsequent calls fail fast with <see cref="CircuitBreakerOpenException"/>
/// instead of waiting for the API to timeout — protecting our thread pool and
/// giving the downstream service time to recover.
///
/// The decorator follows the Decorator pattern: same interface, adds behavior,
/// delegates to the inner (real) implementation.
///
/// Graceful degradation: Callers (Workers, Commands) should catch
/// <see cref="CircuitBreakerOpenException"/> and either:
///   - Queue the work for later retry
///   - Return a partial/cached response
///   - Return a user-friendly "AI is temporarily unavailable" message
/// </summary>
public sealed class ResilientAiServiceDecorator : IAiService
{
    private readonly IAiService _inner;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly ILogger<ResilientAiServiceDecorator> _logger;

    public ResilientAiServiceDecorator(
        IAiService inner,
        ICircuitBreakerFactory circuitBreakerFactory,
        ILogger<ResilientAiServiceDecorator> logger)
    {
        _inner = inner;
        _circuitBreaker = circuitBreakerFactory.GetBreaker("GroqAi");
        _logger = logger;
    }

    public async Task<string> GeneratePortfolioDataAsync(string resumeText)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(
                () => _inner.GeneratePortfolioDataAsync(resumeText));
        }
        catch (CircuitBreakerOpenException ex)
        {
            _logger.LogWarning(
                "AI service call blocked by circuit breaker '{BreakerName}'. Retry after {RetryAfter:O}",
                ex.BreakerName, ex.RetryAfter);
            throw; // Let the caller handle graceful degradation
        }
    }
}

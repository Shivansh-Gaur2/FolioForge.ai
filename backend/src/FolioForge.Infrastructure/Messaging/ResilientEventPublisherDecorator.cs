using FolioForge.Domain.Interfaces;
using FolioForge.Infrastructure.Resilience.CircuitBreaker;
using Microsoft.Extensions.Logging;

namespace FolioForge.Infrastructure.Messaging;

/// <summary>
/// Decorator that wraps <see cref="IEventPublisher"/> with Circuit Breaker protection.
///
/// When RabbitMQ is down (connection refused, channel errors), the circuit opens
/// and subsequent publish calls fail fast instead of blocking on connection timeouts.
///
/// Graceful degradation strategies when the circuit is open:
///   - Store events in a local fallback queue (persistent retry)
///   - Write events to a dead-letter table in SQL Server
///   - Log the event and alert ops for manual recovery
///
/// This decorator currently logs and re-throws, allowing the calling code
/// to decide the degradation strategy.
/// </summary>
public sealed class ResilientEventPublisherDecorator : IEventPublisher
{
    private readonly IEventPublisher _inner;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly ILogger<ResilientEventPublisherDecorator> _logger;

    public ResilientEventPublisherDecorator(
        IEventPublisher inner,
        ICircuitBreakerFactory circuitBreakerFactory,
        ILogger<ResilientEventPublisherDecorator> logger)
    {
        _inner = inner;
        _circuitBreaker = circuitBreakerFactory.GetBreaker("RabbitMq");
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event) where T : class
    {
        try
        {
            await _circuitBreaker.ExecuteAsync(
                () => _inner.PublishAsync(@event));
        }
        catch (CircuitBreakerOpenException ex)
        {
            _logger.LogError(
                "Event publish blocked by circuit breaker '{BreakerName}'. Event type: {EventType}. Retry after {RetryAfter:O}",
                ex.BreakerName, typeof(T).Name, ex.RetryAfter);
            throw; // Caller decides: retry later, dead-letter, or swallow
        }
    }
}

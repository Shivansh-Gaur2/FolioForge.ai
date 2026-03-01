using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FolioForge.Infrastructure.Resilience.CircuitBreaker;

/// <summary>
/// Factory for creating and managing named circuit breaker instances.
///
/// Each downstream dependency gets its own circuit breaker (e.g., "GroqAi", "RabbitMq").
/// Instances are created lazily on first access and cached for the application's lifetime.
///
/// The factory is registered as a singleton in DI. Circuit breakers are also singletons
/// by nature — the state machine must be shared across all requests.
/// </summary>
public interface ICircuitBreakerFactory : IDisposable
{
    /// <summary>Gets or creates a named circuit breaker instance.</summary>
    CircuitBreaker GetBreaker(string name);

    /// <summary>Returns snapshots of all circuit breakers for monitoring/health checks.</summary>
    IReadOnlyDictionary<string, CircuitBreakerSnapshot> GetAllSnapshots();
}

public sealed class CircuitBreakerFactory : ICircuitBreakerFactory
{
    private readonly ConcurrentDictionary<string, CircuitBreaker> _breakers = new(StringComparer.OrdinalIgnoreCase);
    private readonly IOptionsMonitor<CircuitBreakerOptions> _optionsMonitor;
    private readonly ILoggerFactory _loggerFactory;

    public CircuitBreakerFactory(
        IOptionsMonitor<CircuitBreakerOptions> optionsMonitor,
        ILoggerFactory loggerFactory)
    {
        _optionsMonitor = optionsMonitor;
        _loggerFactory = loggerFactory;
    }

    public CircuitBreaker GetBreaker(string name)
    {
        return _breakers.GetOrAdd(name, breakerName =>
        {
            var options = _optionsMonitor.CurrentValue;

            var config = options.Breakers.TryGetValue(breakerName, out var configured)
                ? configured
                : new BreakerConfig(); // sensible defaults

            var logger = _loggerFactory.CreateLogger($"CircuitBreaker.{breakerName}");

            logger.LogInformation(
                "Created circuit breaker '{BreakerName}': FailureThreshold={Threshold}, OpenDuration={OpenDuration}s, HalfOpenProbes={Probes}",
                breakerName,
                config.FailureThreshold,
                config.OpenDurationSeconds,
                config.HalfOpenMaxAttempts);

            return new CircuitBreaker(breakerName, config, logger);
        });
    }

    public IReadOnlyDictionary<string, CircuitBreakerSnapshot> GetAllSnapshots()
    {
        return _breakers.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.GetSnapshot());
    }

    public void Dispose()
    {
        foreach (var breaker in _breakers.Values)
        {
            breaker.Dispose();
        }
        _breakers.Clear();
    }
}

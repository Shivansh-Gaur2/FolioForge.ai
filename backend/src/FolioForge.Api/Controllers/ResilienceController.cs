using FolioForge.Infrastructure.Resilience.Bulkhead;
using FolioForge.Infrastructure.Resilience.CircuitBreaker;
using Microsoft.AspNetCore.Mvc;

namespace FolioForge.Api.Controllers;

/// <summary>
/// Health/monitoring endpoint for resilience infrastructure.
/// Exposes the current state of all circuit breakers and bulkhead partitions.
///
/// This is invaluable for ops dashboards, alerting, and incident response.
/// In production, consider securing this behind an internal-only route or API key.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ResilienceController : ControllerBase
{
    private readonly ICircuitBreakerFactory _circuitBreakerFactory;
    private readonly BulkheadPartitionManager _bulkheadPartitionManager;

    public ResilienceController(
        ICircuitBreakerFactory circuitBreakerFactory,
        BulkheadPartitionManager bulkheadPartitionManager)
    {
        _circuitBreakerFactory = circuitBreakerFactory;
        _bulkheadPartitionManager = bulkheadPartitionManager;
    }

    /// <summary>
    /// GET /api/resilience
    /// Returns current state of all circuit breakers and bulkhead partitions.
    /// </summary>
    [HttpGet]
    public IActionResult GetStatus()
    {
        var circuitBreakers = _circuitBreakerFactory.GetAllSnapshots()
            .Select(kvp => new
            {
                name = kvp.Key,
                state = kvp.Value.State.ToString(),
                consecutiveFailures = kvp.Value.ConsecutiveFailures,
                failureThreshold = kvp.Value.FailureThreshold,
                retryAfter = kvp.Value.State == CircuitBreakerState.Open
                    ? kvp.Value.RetryAfter.ToString("O")
                    : null
            });

        var bulkheadPartitions = _bulkheadPartitionManager.GetSnapshot()
            .Select(kvp => new
            {
                name = kvp.Key,
                activeCount = kvp.Value.ActiveCount,
                queuedCount = kvp.Value.QueuedCount,
                maxConcurrency = kvp.Value.MaxConcurrency,
                maxQueueSize = kvp.Value.MaxQueueSize,
                utilizationPercent = Math.Round(kvp.Value.Utilization * 100, 1)
            });

        return Ok(new
        {
            timestamp = DateTimeOffset.UtcNow,
            circuitBreakers,
            bulkheadPartitions
        });
    }
}

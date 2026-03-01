using Microsoft.Extensions.Logging;

namespace FolioForge.Infrastructure.Resilience.CircuitBreaker;

/// <summary>
/// Thread-safe Circuit Breaker implementation with lock-free fast path.
///
/// Design choices:
/// ───────────────
/// 1. The "hot path" (checking state when circuit is CLOSED) uses Volatile.Read
///    with zero locking — no contention under normal operation.
///
/// 2. State transitions use a dedicated lock to serialize mutations.
///    Contention only occurs during transitions (rare) and half-open probing.
///
/// 3. Half-open probing uses a SemaphoreSlim to limit the number of concurrent
///    probe requests, preventing a thundering herd from hitting a recovering service.
///
/// 4. Failure counting uses Interlocked.Increment for the happy path (closed state)
///    and resets under the lock during state transitions.
///
/// 5. Clock injection via Func&lt;DateTimeOffset&gt; for testability (set to 
///    DateTimeOffset.UtcNow in production).
/// </summary>
public sealed class CircuitBreaker : IDisposable
{
    private readonly string _name;
    private readonly BreakerConfig _config;
    private readonly ILogger _logger;
    private readonly Func<DateTimeOffset> _clock;

    // State machine
    private int _state = (int)CircuitBreakerState.Closed;
    private readonly object _transitionLock = new();

    // Closed state tracking
    private int _consecutiveFailureCount;

    // Open state tracking
    private DateTimeOffset _openedAt;
    private DateTimeOffset _retryAfter;

    // Half-open state tracking
    private readonly SemaphoreSlim _halfOpenGate;
    private int _halfOpenSuccessCount;
    private int _halfOpenAttemptCount;

    public string Name => _name;
    public CircuitBreakerState State => (CircuitBreakerState)Interlocked.CompareExchange(ref _state, 0, 0);

    public CircuitBreaker(
        string name,
        BreakerConfig config,
        ILogger logger,
        Func<DateTimeOffset>? clock = null)
    {
        _name = name;
        _config = config;
        _logger = logger;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _halfOpenGate = new SemaphoreSlim(_config.HalfOpenMaxAttempts, _config.HalfOpenMaxAttempts);
    }

    /// <summary>
    /// Executes an action through the circuit breaker.
    /// Throws <see cref="CircuitBreakerOpenException"/> if the circuit is open.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var state = State;

        switch (state)
        {
            case CircuitBreakerState.Closed:
                return await ExecuteInClosedState(action);

            case CircuitBreakerState.Open:
                return HandleOpenState<T>();

            case CircuitBreakerState.HalfOpen:
                return await ExecuteInHalfOpenState(action, cancellationToken);

            default:
                throw new InvalidOperationException($"Unknown circuit breaker state: {state}");
        }
    }

    /// <summary>
    /// Executes a void action through the circuit breaker.
    /// </summary>
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await action();
            return true; // dummy return for the generic overload
        }, cancellationToken);
    }

    /// <summary>Returns a snapshot of the circuit breaker's current state for monitoring.</summary>
    public CircuitBreakerSnapshot GetSnapshot() => new(
        _name,
        State,
        Volatile.Read(ref _consecutiveFailureCount),
        _config.FailureThreshold,
        _retryAfter);

    // ─────────────────────────────────────────────────────────────────
    // STATE HANDLERS
    // ─────────────────────────────────────────────────────────────────

    private async Task<T> ExecuteInClosedState<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            OnSuccess();
            return result;
        }
        catch (Exception ex) when (ShouldHandle(ex))
        {
            OnFailure(ex);
            throw;
        }
    }

    private T HandleOpenState<T>()
    {
        var now = _clock();

        // Check if cooldown has elapsed → transition to half-open
        if (now >= _retryAfter)
        {
            TransitionTo(CircuitBreakerState.HalfOpen);
            // Fall through to allow this request as a probe
            // (will be picked up on next call or we can handle it here)
        }
        else
        {
            throw new CircuitBreakerOpenException(_name, _retryAfter);
        }

        // After transitioning, this request should be treated as half-open probe
        // Throw so caller retries, which will hit HalfOpen path
        throw new CircuitBreakerOpenException(_name, _retryAfter,
            $"Circuit breaker '{_name}' transitioning to half-open. Retry immediately.");
    }

    private async Task<T> ExecuteInHalfOpenState<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        // Limit concurrent probes
        if (!await _halfOpenGate.WaitAsync(0, cancellationToken))
        {
            // Too many concurrent probes — reject this one
            throw new CircuitBreakerOpenException(_name, _retryAfter,
                $"Circuit breaker '{_name}' is half-open and at max probe capacity.");
        }

        try
        {
            Interlocked.Increment(ref _halfOpenAttemptCount);
            var result = await action();

            var successCount = Interlocked.Increment(ref _halfOpenSuccessCount);
            if (successCount >= _config.SuccessThresholdInHalfOpen)
            {
                TransitionTo(CircuitBreakerState.Closed);
            }

            return result;
        }
        catch (Exception ex) when (ShouldHandle(ex))
        {
            // Probe failed → re-open the circuit
            TransitionTo(CircuitBreakerState.Open);
            throw;
        }
        finally
        {
            _halfOpenGate.Release();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // STATE TRANSITIONS
    // ─────────────────────────────────────────────────────────────────

    private void OnSuccess()
    {
        // Reset failure counter on success (only meaningful in Closed state)
        if (Volatile.Read(ref _consecutiveFailureCount) > 0)
        {
            Interlocked.Exchange(ref _consecutiveFailureCount, 0);
        }
    }

    private void OnFailure(Exception ex)
    {
        var count = Interlocked.Increment(ref _consecutiveFailureCount);

        _logger.LogWarning(ex,
            "Circuit breaker '{BreakerName}' recorded failure {Count}/{Threshold}",
            _name, count, _config.FailureThreshold);

        if (count >= _config.FailureThreshold)
        {
            TransitionTo(CircuitBreakerState.Open);
        }
    }

    private void TransitionTo(CircuitBreakerState newState)
    {
        lock (_transitionLock)
        {
            var currentState = State;

            // Prevent redundant transitions
            if (currentState == newState)
                return;

            // Validate transition legality
            if (!IsValidTransition(currentState, newState))
            {
                _logger.LogDebug(
                    "Circuit breaker '{BreakerName}' ignoring invalid transition {From} → {To}",
                    _name, currentState, newState);
                return;
            }

            switch (newState)
            {
                case CircuitBreakerState.Open:
                    var now = _clock();
                    _openedAt = now;
                    _retryAfter = now.AddSeconds(_config.OpenDurationSeconds);
                    _consecutiveFailureCount = 0;
                    _logger.LogError(
                        "Circuit breaker '{BreakerName}' OPENED. Will retry after {RetryAfter:O}. Previous state: {PreviousState}",
                        _name, _retryAfter, currentState);
                    break;

                case CircuitBreakerState.HalfOpen:
                    _halfOpenSuccessCount = 0;
                    _halfOpenAttemptCount = 0;
                    _logger.LogWarning(
                        "Circuit breaker '{BreakerName}' entering HALF-OPEN state. Allowing {MaxProbes} probe requests",
                        _name, _config.HalfOpenMaxAttempts);
                    break;

                case CircuitBreakerState.Closed:
                    _consecutiveFailureCount = 0;
                    _halfOpenSuccessCount = 0;
                    _halfOpenAttemptCount = 0;
                    _logger.LogInformation(
                        "Circuit breaker '{BreakerName}' CLOSED. Normal operation resumed",
                        _name);
                    break;
            }

            Interlocked.Exchange(ref _state, (int)newState);
        }
    }

    private static bool IsValidTransition(CircuitBreakerState from, CircuitBreakerState to) =>
        (from, to) switch
        {
            (CircuitBreakerState.Closed, CircuitBreakerState.Open) => true,
            (CircuitBreakerState.Open, CircuitBreakerState.HalfOpen) => true,
            (CircuitBreakerState.HalfOpen, CircuitBreakerState.Closed) => true,
            (CircuitBreakerState.HalfOpen, CircuitBreakerState.Open) => true,
            _ => false
        };

    private bool ShouldHandle(Exception ex)
    {
        // If no specific types are configured, handle ALL exceptions
        if (_config.HandledExceptionTypes.Count == 0)
            return true;

        var exceptionTypeName = ex.GetType().FullName ?? ex.GetType().Name;
        return _config.HandledExceptionTypes
            .Any(t => exceptionTypeName.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        _halfOpenGate.Dispose();
    }
}

/// <summary>Read-only snapshot of a circuit breaker's state for monitoring/health checks.</summary>
public sealed record CircuitBreakerSnapshot(
    string Name,
    CircuitBreakerState State,
    int ConsecutiveFailures,
    int FailureThreshold,
    DateTimeOffset RetryAfter);

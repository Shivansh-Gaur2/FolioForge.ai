using FluentAssertions;
using FolioForge.Infrastructure.Resilience.CircuitBreaker;
using Microsoft.Extensions.Logging;
using Moq;

namespace FolioForge.Tests.Resilience;

/// <summary>
/// Unit tests for the Circuit Breaker state machine.
/// Uses a controllable clock to avoid real time delays.
/// </summary>
public class CircuitBreakerTests
{
    private readonly Mock<ILogger> _loggerMock = new();
    private DateTimeOffset _now = DateTimeOffset.UtcNow;
    private DateTimeOffset Clock() => _now;

    private CircuitBreaker CreateBreaker(int failureThreshold = 3, double openDurationSeconds = 10)
    {
        var config = new BreakerConfig
        {
            FailureThreshold = failureThreshold,
            OpenDurationSeconds = openDurationSeconds,
            HalfOpenMaxAttempts = 2,
            SuccessThresholdInHalfOpen = 2
        };
        return new CircuitBreaker("test", config, _loggerMock.Object, Clock);
    }

    [Fact]
    public void NewBreaker_ShouldBeClosed()
    {
        var breaker = CreateBreaker();
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task SuccessfulCall_ShouldReturnResult()
    {
        var breaker = CreateBreaker();
        var result = await breaker.ExecuteAsync(() => Task.FromResult(42));
        result.Should().Be(42);
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ConsecutiveFailures_ShouldOpenCircuit()
    {
        var breaker = CreateBreaker(failureThreshold: 3);

        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("boom")));
        }

        breaker.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task OpenCircuit_ShouldRejectCalls()
    {
        var breaker = CreateBreaker(failureThreshold: 1);

        // Trip the breaker
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("boom")));

        breaker.State.Should().Be(CircuitBreakerState.Open);

        // Next call should be rejected with CircuitBreakerOpenException
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => breaker.ExecuteAsync(() => Task.FromResult(42)));
    }

    [Fact]
    public async Task OpenCircuit_TransitionsToHalfOpenAfterCooldown()
    {
        var breaker = CreateBreaker(failureThreshold: 1, openDurationSeconds: 10);

        // Trip the breaker
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("boom")));
        breaker.State.Should().Be(CircuitBreakerState.Open);

        // Advance clock past the open duration
        _now = _now.AddSeconds(11);

        // HandleOpenState transitions to HalfOpen but still throws; the NEXT call goes through HalfOpen path
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => breaker.ExecuteAsync(() => Task.FromResult(1)));

        breaker.State.Should().Be(CircuitBreakerState.HalfOpen);
    }

    [Fact]
    public async Task HalfOpen_SuccessfulProbes_ClosesCircuit()
    {
        var breaker = CreateBreaker(failureThreshold: 1, openDurationSeconds: 10);

        // Trip + transition to half-open
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("boom")));
        _now = _now.AddSeconds(11);
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => breaker.ExecuteAsync(() => Task.FromResult(1)));
        breaker.State.Should().Be(CircuitBreakerState.HalfOpen);

        // SuccessThresholdInHalfOpen = 2, so we need 2 successful probes
        await breaker.ExecuteAsync(() => Task.FromResult(1));
        await breaker.ExecuteAsync(() => Task.FromResult(2));

        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task HalfOpen_FailedProbe_ReopensCircuit()
    {
        var breaker = CreateBreaker(failureThreshold: 1, openDurationSeconds: 10);

        // Trip + transition to half-open
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("boom")));
        _now = _now.AddSeconds(11);
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => breaker.ExecuteAsync(() => Task.FromResult(1)));
        breaker.State.Should().Be(CircuitBreakerState.HalfOpen);

        // Probe fails → re-open
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("boom")));

        breaker.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task SuccessAfterFailures_ShouldResetFailureCount()
    {
        var breaker = CreateBreaker(failureThreshold: 3);

        // 2 failures
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("boom")));
        }

        // 1 success — should reset the counter
        await breaker.ExecuteAsync(() => Task.FromResult(1));

        // 2 more failures should NOT open the circuit (counter was reset)
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => breaker.ExecuteAsync<int>(() => throw new InvalidOperationException("boom")));
        }

        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void Snapshot_ShouldReflectCurrentState()
    {
        var breaker = CreateBreaker(failureThreshold: 5);
        var snapshot = breaker.GetSnapshot();

        snapshot.Name.Should().Be("test");
        snapshot.State.Should().Be(CircuitBreakerState.Closed);
        snapshot.FailureThreshold.Should().Be(5);
        snapshot.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var breaker = CreateBreaker();
        var act = () => breaker.Dispose();
        act.Should().NotThrow();
    }
}

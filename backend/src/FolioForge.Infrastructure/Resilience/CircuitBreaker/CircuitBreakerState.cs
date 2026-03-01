namespace FolioForge.Infrastructure.Resilience.CircuitBreaker;

/// <summary>
/// Circuit breaker state machine states.
///
///   ┌──────────┐  failures >= threshold   ┌──────┐  cooldown elapsed   ┌──────────┐
///   │  CLOSED  │ ──────────────────────▶  │ OPEN │ ──────────────────▶ │ HALF_OPEN│
///   │ (normal) │                          │(fail)│                     │ (probing)│
///   └──────────┘                          └──────┘                     └──────────┘
///        ▲                                    ▲                            │
///        │           success                  │        failure             │
///        └────────────────────────────────────┼────────────────────────────┘
///                                             │
///                                          (re-open)
///
/// CLOSED:    Normal operation. Every failure increments a counter.
///            When failures reach the threshold, transition to OPEN.
///            A success resets the failure counter.
///
/// OPEN:      All calls are rejected immediately (fail-fast).
///            After the cooldown duration elapses, transition to HALF_OPEN.
///
/// HALF_OPEN: A limited number of "probe" calls are allowed through.
///            If the probe succeeds, transition back to CLOSED.
///            If the probe fails, transition back to OPEN with a fresh cooldown.
/// </summary>
public enum CircuitBreakerState
{
    Closed = 0,
    Open = 1,
    HalfOpen = 2
}

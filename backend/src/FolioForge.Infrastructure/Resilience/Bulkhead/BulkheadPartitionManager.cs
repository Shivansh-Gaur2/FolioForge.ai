using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FolioForge.Infrastructure.Resilience.Bulkhead;

/// <summary>
/// A single Bulkhead partition backed by a <see cref="SemaphoreSlim"/>.
///
/// The semaphore's initial count = MaxConcurrency + MaxQueueSize.
/// This models both the "active" slots and the "waiting" queue as a single semaphore.
///
/// When a request arrives:
///   1. Try to enter the semaphore with a timeout (QueueTimeoutMs).
///   2. If successful → request proceeds. If not → request is rejected.
///
/// The distinction between "active" and "queued" is tracked by a separate
/// Interlocked counter (_activeCount) so we can report accurate metrics.
/// </summary>
public sealed class BulkheadPartition : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly PartitionOptions _options;
    private int _activeCount;
    private int _queuedCount;

    public string Name { get; }

    /// <summary>Currently executing requests in this partition.</summary>
    public int ActiveCount => Volatile.Read(ref _activeCount);

    /// <summary>Requests currently waiting in the queue.</summary>
    public int QueuedCount => Volatile.Read(ref _queuedCount);

    /// <summary>Maximum concurrent requests allowed.</summary>
    public int MaxConcurrency => _options.MaxConcurrency;

    /// <summary>Maximum queue depth.</summary>
    public int MaxQueueSize => _options.MaxQueueSize;

    public BulkheadPartition(string name, PartitionOptions options)
    {
        Name = name;
        _options = options;

        // Total capacity = concurrency slots + queue slots
        var totalCapacity = options.MaxConcurrency + options.MaxQueueSize;
        _semaphore = new SemaphoreSlim(totalCapacity, totalCapacity);
    }

    /// <summary>
    /// Attempts to enter this partition. Returns a disposable lease if successful.
    /// Returns null if the partition is full (all concurrency + queue slots exhausted)
    /// or the queue timeout expires.
    /// </summary>
    public async Task<BulkheadLease?> TryEnterAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _queuedCount);

        try
        {
            var acquired = await _semaphore.WaitAsync(
                TimeSpan.FromMilliseconds(_options.QueueTimeoutMs),
                cancellationToken);

            if (!acquired)
            {
                return null;
            }
        }
        finally
        {
            Interlocked.Decrement(ref _queuedCount);
        }

        Interlocked.Increment(ref _activeCount);
        return new BulkheadLease(this);
    }

    internal void Release()
    {
        Interlocked.Decrement(ref _activeCount);
        _semaphore.Release();
    }

    public void Dispose() => _semaphore.Dispose();
}

/// <summary>
/// Disposable lease representing an acquired slot in a Bulkhead partition.
/// The slot is released back to the partition when disposed.
/// </summary>
public sealed class BulkheadLease : IDisposable
{
    private readonly BulkheadPartition _partition;
    private int _disposed;

    internal BulkheadLease(BulkheadPartition partition)
    {
        _partition = partition;
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            _partition.Release();
        }
    }
}

/// <summary>
/// Manages all Bulkhead partitions. Thread-safe, lazily creates partitions on first access.
/// Registered as a singleton — partitions live for the lifetime of the application.
///
/// Why per-instance (not distributed)?
/// Each API server has its own bounded resources (thread pool, memory, CPU).
/// A bulkhead protects THIS server from overloading, not the cluster as a whole.
/// Rate limiting (via Redis) handles cluster-wide throttling.
/// Bulkhead handles local resource isolation.
/// </summary>
public sealed class BulkheadPartitionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, BulkheadPartition> _partitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly IOptionsMonitor<BulkheadOptions> _optionsMonitor;
    private readonly ILogger<BulkheadPartitionManager> _logger;

    public BulkheadPartitionManager(
        IOptionsMonitor<BulkheadOptions> optionsMonitor,
        ILogger<BulkheadPartitionManager> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a partition by name. Thread-safe via ConcurrentDictionary.
    /// Falls back to default options if the named partition isn't configured.
    /// </summary>
    public BulkheadPartition GetPartition(string name)
    {
        return _partitions.GetOrAdd(name, partitionName =>
        {
            var options = _optionsMonitor.CurrentValue;

            var partitionOptions = options.Partitions.TryGetValue(partitionName, out var configured)
                ? configured
                : new PartitionOptions(); // sensible defaults

            _logger.LogInformation(
                "Created bulkhead partition '{PartitionName}': MaxConcurrency={MaxConcurrency}, MaxQueue={MaxQueue}, QueueTimeout={QueueTimeout}ms",
                partitionName,
                partitionOptions.MaxConcurrency,
                partitionOptions.MaxQueueSize,
                partitionOptions.QueueTimeoutMs);

            return new BulkheadPartition(partitionName, partitionOptions);
        });
    }

    /// <summary>Returns a snapshot of all active partitions for monitoring/health checks.</summary>
    public IReadOnlyDictionary<string, BulkheadPartitionSnapshot> GetSnapshot()
    {
        return _partitions.ToDictionary(
            kvp => kvp.Key,
            kvp => new BulkheadPartitionSnapshot(
                kvp.Value.Name,
                kvp.Value.ActiveCount,
                kvp.Value.QueuedCount,
                kvp.Value.MaxConcurrency,
                kvp.Value.MaxQueueSize));
    }

    public void Dispose()
    {
        foreach (var partition in _partitions.Values)
        {
            partition.Dispose();
        }
        _partitions.Clear();
    }
}

/// <summary>Read-only snapshot of a partition's state for monitoring.</summary>
public sealed record BulkheadPartitionSnapshot(
    string Name,
    int ActiveCount,
    int QueuedCount,
    int MaxConcurrency,
    int MaxQueueSize)
{
    public double Utilization => MaxConcurrency > 0
        ? (double)ActiveCount / MaxConcurrency
        : 0;
}

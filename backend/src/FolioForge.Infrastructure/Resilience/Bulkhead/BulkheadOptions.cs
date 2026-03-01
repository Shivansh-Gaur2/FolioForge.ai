namespace FolioForge.Infrastructure.Resilience.Bulkhead;

/// <summary>
/// Configuration options for the Bulkhead pattern.
/// Bound from appsettings.json section "Bulkhead".
/// </summary>
public sealed class BulkheadOptions
{
    public const string SectionName = "Bulkhead";

    /// <summary>Master switch to enable/disable bulkhead enforcement globally.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Named partitions. Each partition gets its own concurrency pool.
    /// Requests to a partition are limited to MaxConcurrency active + MaxQueueSize waiting.
    /// </summary>
    public Dictionary<string, PartitionOptions> Partitions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Configuration for a single Bulkhead partition.
///
/// MaxConcurrency: How many requests can execute simultaneously in this partition.
///   Set this based on the resource cost of requests in this group.
///   CPU-bound endpoints → lower. I/O-bound endpoints → higher.
///
/// MaxQueueSize: How many requests can wait in line when all concurrency slots are taken.
///   Once the queue is full, new requests are immediately rejected with 503.
///   Set to 0 for "reject immediately when busy" behavior.
///
/// QueueTimeoutMs: Maximum time a request waits in the queue before being rejected.
///   Prevents requests from waiting forever, even if a slot never opens.
/// </summary>
public sealed class PartitionOptions
{
    /// <summary>Maximum number of concurrent requests in this partition.</summary>
    public int MaxConcurrency { get; set; } = 50;

    /// <summary>Maximum number of requests waiting in the queue.</summary>
    public int MaxQueueSize { get; set; } = 100;

    /// <summary>Maximum time (in milliseconds) a request waits in the queue.</summary>
    public int QueueTimeoutMs { get; set; } = 5000;
}

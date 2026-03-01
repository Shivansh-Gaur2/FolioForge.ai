namespace FolioForge.Infrastructure.Resilience.Bulkhead;

/// <summary>
/// Attribute to assign an endpoint to a named Bulkhead partition.
///
/// The Bulkhead pattern isolates endpoint groups into separate concurrency pools.
/// If one partition (e.g., "Upload") is saturated with slow requests, other
/// partitions (e.g., "Default" reads) continue to operate normally.
///
/// Think of it like compartments on a ship: a breach in one compartment
/// doesn't sink the entire vessel.
///
/// Usage:
///   [Bulkhead("Upload")]          → isolates into the "Upload" partition
///   [Bulkhead("AiProcessing")]    → isolates into the "AiProcessing" partition
///   [Bulkhead(Disabled = true)]   → exempts this endpoint from bulkhead enforcement
///
/// When applied to both controller and action, the action-level attribute wins.
/// If no attribute is present, the middleware falls back to the "Default" partition.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class BulkheadAttribute : Attribute
{
    /// <summary>Name of the bulkhead partition.</summary>
    public string PartitionName { get; }

    /// <summary>Set to true to bypass bulkhead enforcement for this endpoint.</summary>
    public bool Disabled { get; set; }

    public BulkheadAttribute(string partitionName = "Default")
    {
        PartitionName = partitionName;
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace FolioForge.Infrastructure.Resilience.Bulkhead;

/// <summary>
/// ASP.NET Core middleware that enforces the Bulkhead pattern.
///
/// Purpose:
/// ────────
/// Prevents one category of endpoints from consuming all server resources
/// and starving other categories. If the "Upload" partition is fully saturated
/// (all concurrency slots + queue full), new upload requests are rejected with
/// HTTP 503 Service Unavailable, but reads/auth/other partitions continue normally.
///
/// Pipeline position:
/// ──────────────────
/// Placed AFTER rate limiting (so rate-limited requests never consume bulkhead slots)
/// and AFTER authentication (so we know who the user is for logging).
///
/// Response behavior:
/// ──────────────────
/// When rejected, returns HTTP 503 with a JSON body containing the partition name
/// and a Retry-After header suggesting when to retry.
/// </summary>
public sealed class BulkheadMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BulkheadPartitionManager _partitionManager;
    private readonly IOptionsMonitor<BulkheadOptions> _optionsMonitor;
    private readonly ILogger<BulkheadMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly string[] AlwaysExcludedPaths =
    [
        "/health",
        "/healthz",
        "/metrics",
        "/swagger"
    ];

    public BulkheadMiddleware(
        RequestDelegate next,
        BulkheadPartitionManager partitionManager,
        IOptionsMonitor<BulkheadOptions> optionsMonitor,
        ILogger<BulkheadMiddleware> logger)
    {
        _next = next;
        _partitionManager = partitionManager;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var options = _optionsMonitor.CurrentValue;

        // Master switch
        if (!options.Enabled)
        {
            await _next(context);
            return;
        }

        // Skip infrastructure endpoints
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        // Determine which partition this endpoint belongs to
        var partitionName = ResolvePartitionName(context);
        if (partitionName is null)
        {
            // [Bulkhead(Disabled = true)] on this endpoint
            await _next(context);
            return;
        }

        var partition = _partitionManager.GetPartition(partitionName);

        // Try to acquire a slot in the partition
        BulkheadLease? lease = null;
        try
        {
            lease = await partition.TryEnterAsync(context.RequestAborted);

            if (lease is null)
            {
                await WriteRejectedResponse(context, partition, partitionName);
                return;
            }

            // Slot acquired — execute the request
            await _next(context);
        }
        finally
        {
            lease?.Dispose();
        }
    }

    /// <summary>
    /// Resolves the bulkhead partition name from endpoint metadata.
    /// Returns null if bulkhead is explicitly disabled for this endpoint.
    /// </summary>
    private static string? ResolvePartitionName(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            return "Default";
        }

        var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (actionDescriptor is not null)
        {
            // Action-level attribute takes priority
            var actionAttr = actionDescriptor.MethodInfo.GetCustomAttribute<BulkheadAttribute>();
            if (actionAttr is not null)
            {
                return actionAttr.Disabled ? null : actionAttr.PartitionName;
            }

            // Controller-level attribute
            var controllerAttr = actionDescriptor.ControllerTypeInfo.GetCustomAttribute<BulkheadAttribute>();
            if (controllerAttr is not null)
            {
                return controllerAttr.Disabled ? null : controllerAttr.PartitionName;
            }
        }

        // No attribute → use Default partition
        return "Default";
    }

    private static bool IsExcludedPath(string path)
    {
        foreach (var excluded in AlwaysExcludedPaths)
        {
            if (path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private async Task WriteRejectedResponse(
        HttpContext context,
        BulkheadPartition partition,
        string partitionName)
    {
        _logger.LogWarning(
            "Bulkhead rejected: {Method} {Path} | Partition: {PartitionName} | Active: {Active}/{MaxConcurrency} | Queued: {Queued}/{MaxQueue}",
            context.Request.Method,
            context.Request.Path,
            partitionName,
            partition.ActiveCount,
            partition.MaxConcurrency,
            partition.QueuedCount,
            partition.MaxQueueSize);

        context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
        context.Response.ContentType = "application/json";
        context.Response.Headers["Retry-After"] = "5"; // suggest retry in 5 seconds

        var body = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            title = "Service Unavailable",
            status = 503,
            detail = $"Server is too busy to handle this request. The '{partitionName}' processing partition is at capacity.",
            partition = partitionName,
            retryAfterSeconds = 5
        };

        await context.Response.WriteAsJsonAsync(body, JsonOptions, context.RequestAborted);
    }
}

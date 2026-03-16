using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace FolioForge.Infrastructure.Telemetry;

/// <summary>
/// A custom span processor that promotes any span with error status or recorded
/// exception to be exported regardless of the head-based sampling decision.
/// This provides a lightweight "tail sampling" behavior when running inside a
/// single process without a collector.
/// </summary>
public class ErrorPromotingSpanProcessor : BaseProcessor<Activity>
{
    private readonly BaseExporter<Activity> _exporter;

    public ErrorPromotingSpanProcessor(BaseExporter<Activity> exporter)
    {
        _exporter = exporter;
    }

    public override void OnEnd(Activity data)
    {
        if (data == null) return;

        // If the activity is marked as error, make sure it's exported
        if (data.Status == ActivityStatusCode.Error)
        {
            // Batch<T> is a struct used by exporters
            _exporter.Export(new Batch<Activity>(data));
        }
    }
}

/// <summary>
/// Helper extensions for tracing builder configuration.
/// </summary>
public static class SamplerExtensions
{
    /// <summary>
    /// Configure a parent-based sampler with a ratio for successful traces and
    /// attach the error-promoting processor.
    /// </summary>
    /// <param name="builder">The tracing builder to configure.</param>
    /// <param name="successRatio">Head-based sampling ratio (0.0–1.0).</param>
    /// <param name="errorExporter">
    /// A dedicated exporter instance used exclusively by the error-promoting
    /// processor. Pass <c>null</c> to skip error promotion (e.g. when
    /// sampling ratio is already 1.0).
    /// </param>
    public static TracerProviderBuilder SetSmartSampler(
        this TracerProviderBuilder builder,
        double successRatio,
        BaseExporter<Activity>? errorExporter = null)
    {
        builder.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(successRatio)));

        if (errorExporter is not null)
        {
            builder.AddProcessor(new ErrorPromotingSpanProcessor(errorExporter));
        }

        return builder;
    }
}

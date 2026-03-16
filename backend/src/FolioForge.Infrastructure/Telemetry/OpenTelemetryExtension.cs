using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FolioForge.Infrastructure.Telemetry;

public static class OpenTelemetryExtension
{
    public static IServiceCollection AddFolioForgeOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Read configuration ──
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "FolioForge";
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
        var successRatio = configuration.GetValue<double>("OpenTelemetry:Sampling:SuccessRatio", 1.0);

        services.AddOpenTelemetry()

            // ── Resource: identifies this service in Jaeger ──
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: typeof(OpenTelemetryExtension).Assembly
                        .GetName().Version?.ToString() ?? "1.0.0"))

            // ── Tracing: spans for every operation ──
            .WithTracing(tracing =>
            {
                tracing
                    // Listen to the custom ActivitySource declared in FolioForgeDiagnostics
                    .AddSource(FolioForgeDiagnostics.ServiceName)

                    // Auto-instrument incoming HTTP requests (ASP.NET Core — no-op in Worker)
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        // Filter out noisy endpoints that clutter Jaeger
                        opts.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/swagger") &&
                            !httpContext.Request.Path.StartsWithSegments("/health") &&
                            !httpContext.Request.Path.StartsWithSegments("/metrics");
                    })

                    // Auto-instrument outgoing HttpClient calls (GroqAI, Gemini)
                    .AddHttpClientInstrumentation()

                    // Auto-instrument SQL queries via EF Core's SqlClient
                    .AddSqlClientInstrumentation(opts =>
                    {
                        opts.SetDbStatementForText = true;
                    })

                    // Auto-instrument StackExchange.Redis operations
                    .AddRedisInstrumentation()

                    // Smart sampling: ParentBased ensures trace consistency across services
                    // - No parent (new trace): sample at successRatio (1% in prod, 100% in dev)
                    // - Has parent (propagated): follow parent's sampling decision
                    //
                    // A dedicated OTLP exporter is created for the error-promoting
                    // processor so it can force-export error spans that were not
                    // sampled by the head-based sampler. This is a separate
                    // connection so it doesn't interfere with the main pipeline.
                    .SetSmartSampler(
                        successRatio,
                        errorExporter: successRatio < 1.0
                            ? new OtlpTraceExporter(new OtlpExporterOptions
                              {
                                  Endpoint = new Uri(otlpEndpoint)
                              })
                            : null)

                    // Export all sampled spans to Jaeger via OTLP gRPC
                    .AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
            })

            // ── Metrics: counters, histograms for dashboards ──
            .WithMetrics(metrics =>
            {
                metrics
                    // Listen to the custom Meter declared in FolioForgeDiagnostics
                    .AddMeter(FolioForgeDiagnostics.ServiceName)

                    // Built-in ASP.NET Core metrics (request duration, active requests)
                    .AddAspNetCoreInstrumentation()

                    // Built-in HttpClient metrics (outgoing request duration)
                    .AddHttpClientInstrumentation()

                    // Expose /metrics endpoint for Prometheus to scrape
                    .AddPrometheusExporter();
            });

        return services;
    }
}
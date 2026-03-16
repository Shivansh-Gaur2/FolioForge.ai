using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

namespace FolioForge.Infrastructure.Telemetry;

public static class RabbitMqContextPropagator
{
    // This class is a placeholder for any custom logic needed to propagate tracing context through RabbitMQ messages.
    // With OpenTelemetry's built-in instrumentation, this is often handled automatically, but you can customize it if needed.
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    /// Called by the publisher to write the current trace context into message headers.
    public static void Inject(Activity? activity, BasicProperties properties)
    {
        // Ensure the Headers dictionary exists
        properties.Headers ??= new Dictionary<string, object?>();

        // The Propagator writes "traceparent" and "tracestate" keys into the headers
        // It calls our setter lambda for each header it needs to write
        Propagator.Inject(
            new PropagationContext(
                activity?.Context ?? default,
                Baggage.Current),
            properties.Headers,
            InjectHeader);
    }

    // Lambda that the propagator calls: "please store this key-value pair"
    // RabbitMQ expects header values as byte[], so we encode the string
    private static void InjectHeader(IDictionary<string, object?> headers, string key, string value)
    {
        headers[key] = Encoding.UTF8.GetBytes(value);
    }

    /// Called by the consumer to read trace context from incoming message headers.
    public static PropagationContext Extract(IReadOnlyBasicProperties properties)
    {
        // If no headers exist, return empty context — the worker will start a fresh trace
        if (properties.Headers is null)
            return default;

        // Convert IReadOnlyDictionary<string, object?> to Dictionary<string, object>
        // because the OTel Propagator.Extract expects IDictionary<string, object>
        var carrier = properties.Headers
            .Where(kv => kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => kv.Value!);

        return Propagator.Extract(
            default,
            carrier,
            ExtractHeader);
    }

    // Lambda that the propagator calls: "give me the value for this key"
    // RabbitMQ stores header values as byte[], so we decode back to string
    private static IEnumerable<string> ExtractHeader(
        IDictionary<string, object> headers, string key)
    {
        if (headers.TryGetValue(key, out var value) && value is byte[] bytes)
        {
            return [Encoding.UTF8.GetString(bytes)];
        }
        return [];
    }
}
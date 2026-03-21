using System.Diagnostics; // Gives me ActivitySource and Activity
using System.Diagnostics.Metrics; // Gives me Counter, Meter, Histogram

namespace FolioForge.Infrastructure.Telemetry;
public static class FolioForgeDiagnostics
{
    public const string ServiceName = "FolioForge"; // Used in telemetry to identify the service emitting the data.
    public static readonly ActivitySource ActivitySource = new ActivitySource(ServiceName); // For tracing operations across the service.
    public const string ProduceMessage = "resume_processing_queue publish";
    public const string ConsumeMessage = "resume_processing_queue process";

    public const string ExtractPdf = "pdf.extract-text";
    public const string GeneratePortfolio = "ai.generate-portfolio";

    public const string CircuitBreakerExecute = "circuit-breaker.execute";

    public static readonly Meter Meter = new Meter(ServiceName); // For emitting custom metrics.
    // Counters — "how many times did X happen?"
    public static readonly Counter<long> MessagesPublished = 
        Meter.CreateCounter<long>(
            name: "folioforge.messages.published",
            unit: "{message}",
            description: "Number of messages published to RabbitMQ");

    public static readonly Counter<long> MessagesProcessed = 
        Meter.CreateCounter<long>(
            name: "folioforge.messages.processed",
            unit: "{message}",
            description: "Number of messages consumed from RabbitMQ");

    // Histogram — "what's the distribution of processing times?"
    public static readonly Histogram<double> ResumeProcessingDuration = 
        Meter.CreateHistogram<double>(
            name: "folioforge.resume.processing_duration",
            unit: "ms",
            description: "Time to process a resume end-to-end in the worker");

    // Counters for resilience
    public static readonly Counter<long> CircuitBreakerTrips = 
        Meter.CreateCounter<long>(
            name: "folioforge.circuit_breaker.trips",
            unit: "{trip}",
            description: "Number of circuit breaker state transitions");

    public static class Tags
    {
        public const string MessagingSystem = "messaging.system";
        public const string MessagingDestination = "messaging.destination.name";
        public const string MessagingOperation = "messaging.operation.type";
        public const string EventType = "folioforge.event.type";
        public const string PortfolioId = "folioforge.portfolio.id";
        public const string CircuitBreakerName = "folioforge.circuit_breaker.name";
        public const string CircuitBreakerState = "folioforge.circuit_breaker.state";
    }
}
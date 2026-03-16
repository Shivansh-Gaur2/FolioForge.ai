using System.Diagnostics;
using FolioForge.Application.Common;
using FolioForge.Application.Common.Events;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Infrastructure.Persistence;
using FolioForge.Infrastructure.Telemetry;
using OpenTelemetry.Trace;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FolioForge.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _rabbitHost;
    private IConnection? _connection;
    private IChannel? _channel;
    // My database is supposed to be scoped , so I need to create a scope to resolve it inside the worker
    // Instead of injecting the IPdfService directly, I inject the IServiceScopeFactory to create a scope when processing each message
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _rabbitHost = configuration["RabbitMq:HostName"] ?? "localhost";
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { HostName = _rabbitHost };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        // Durable queue to match the publisher — messages survive broker restarts
        await _channel.QueueDeclareAsync(queue: "resume_processing_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

        // Prefetch 1: only deliver one unacked message at a time.
        // This prevents the worker from being overwhelmed and ensures fair distribution.
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation(" [*] Waiting for messages. ");

        await base.StartAsync(cancellationToken);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
            throw new InvalidOperationException("Channel not initialized. Ensure StartAsync completed.");

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            // ★ Extract trace context that the publisher injected into message headers
            var parentContext = RabbitMqContextPropagator.Extract(ea.BasicProperties);

            // Start a Consumer span linked to the extracted parent — same TraceId, new SpanId
            using var activity = FolioForgeDiagnostics.ActivitySource.StartActivity(
                FolioForgeDiagnostics.ConsumeMessage,
                ActivityKind.Consumer,
                parentContext: parentContext.ActivityContext);

            activity?.SetTag(FolioForgeDiagnostics.Tags.MessagingSystem, "rabbitmq");
            activity?.SetTag(FolioForgeDiagnostics.Tags.MessagingDestination, "resume_processing_queue");
            activity?.SetTag(FolioForgeDiagnostics.Tags.MessagingOperation, "process");

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Received message from {Queue}", "resume_processing_queue");

            try
            {
                var resumeEvent = JsonSerializer.Deserialize<ResumeUploadedEvent>(message);

                if (resumeEvent != null)
                {
                    activity?.SetTag(FolioForgeDiagnostics.Tags.PortfolioId,
                        resumeEvent.PortfolioId.ToString());

                    _logger.LogInformation(
                        "Processing resume for Portfolio {PortfolioId}",
                        resumeEvent.PortfolioId);

                    var sw = Stopwatch.StartNew();
                    await ProcessResumeAsync(resumeEvent.FilePath, resumeEvent.PortfolioId);
                    sw.Stop();

                    FolioForgeDiagnostics.ResumeProcessingDuration.Record(sw.Elapsed.TotalMilliseconds,
                        new KeyValuePair<string, object?>("status", "success"));

                    // Record success in custom metrics
                    FolioForgeDiagnostics.MessagesProcessed.Add(1,
                        new KeyValuePair<string, object?>("event_type", "ResumeUploadedEvent"),
                        new KeyValuePair<string, object?>("success", "true"));
                }

                // ★ Manual ACK: only acknowledge AFTER successful processing.
                // If we crash before this line, RabbitMQ will redeliver the message.
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // ★ Mark span as ERROR — shows red in Jaeger & triggers smart sampler
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddEvent(new ActivityEvent("exception", default, new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message },
                    { "exception.stacktrace", ex.StackTrace }
                }));

                // record failure metric and duration if available
                FolioForgeDiagnostics.MessagesProcessed.Add(1,
                    new KeyValuePair<string, object?>("event_type", "ResumeUploadedEvent"),
                    new KeyValuePair<string, object?>("success", "false"));
                activity?.SetTag("error", true);

                _logger.LogError(ex, "Error processing message for queue {Queue}",
                    "resume_processing_queue");

                // ★ NACK with requeue=false: send to dead-letter (or discard) so we don't
                // get stuck in an infinite retry loop on poison messages.
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };
        // ★ autoAck: false — we manually ack/nack after processing
        await _channel.BasicConsumeAsync(queue: "resume_processing_queue", autoAck: false, consumer: consumer);

        // Keep the service alive
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

    }

    private async Task ProcessResumeAsync(string filePath, Guid portfolioId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();
            var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

            try
            {
                // 1. Extract & Analyze
                var text = pdfService.ExtractText(filePath);
                _logger.LogInformation("Text extracted from {FilePath}. Calling AI...", filePath);

                var jsonString = await aiService.GeneratePortfolioDataAsync(text);
                _logger.LogInformation("AI data received for Portfolio {PortfolioId}", portfolioId);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<AiResultDto>(jsonString, options);
                if (data is null)
                    throw new InvalidOperationException("Failed to deserialize AI response into AiResultDto.");

                // ==================================================
                // TRANSACTIONAL Nuke & Pave
                // Wrapping delete + insert in a single transaction
                // ensures atomicity — if insert fails, delete is rolled back.
                // ==================================================
                await using var transaction = await dbContext.Database.BeginTransactionAsync();

                try
                {
                    // 2. Fetch and Delete Old Sections
                    var existingSections = await dbContext.Sections
                                                .Where(s => s.PortfolioId == portfolioId)
                                                .ToListAsync();

                    if (existingSections.Any())
                    {
                        dbContext.Sections.RemoveRange(existingSections);
                        await dbContext.SaveChangesAsync();
                    }

                    // 3. Clear the tracker to prevent conflicts
                    dbContext.ChangeTracker.Clear();

                    // 4. Insert New Sections
                    var newSections = new List<PortfolioSection>
                    {
                        new PortfolioSection("About", 1, JsonSerializer.Serialize(new { content = data.Summary }))
                            { PortfolioId = portfolioId },

                        new PortfolioSection("Skills", 2, JsonSerializer.Serialize(new { items = data.Skills }))
                            { PortfolioId = portfolioId },

                        new PortfolioSection("Timeline", 3, JsonSerializer.Serialize(new { items = data.Experience }))
                            { PortfolioId = portfolioId },

                        new PortfolioSection("Projects", 4, JsonSerializer.Serialize(new { items = data.Projects }))
                            { PortfolioId = portfolioId }
                    };

                    await dbContext.Sections.AddRangeAsync(newSections);
                    await dbContext.SaveChangesAsync();

                    // Commit — both delete and insert succeed atomically
                    await transaction.CommitAsync();
                }
                catch
                {
                    // Rollback — old sections are preserved if insert fails
                    await transaction.RollbackAsync();
                    throw;
                }

                // Invalidate Redis cache for this portfolio so the next fetch gets fresh data
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                await cacheService.RemoveAsync(CacheKeys.PortfolioById(portfolioId));
                // Also invalidate the user's portfolio list (fetch userId from the portfolio)
                var portfolio = await dbContext.Portfolios
                    .IgnoreQueryFilters()
                    .Where(p => p.Id == portfolioId)
                    .Select(p => new { p.UserId })
                    .FirstOrDefaultAsync();
                if (portfolio != null)
                {
                    await cacheService.RemoveByPrefixAsync(CacheKeys.PortfoliosByUser(portfolio.UserId));
                }

                _logger.LogInformation("Database updated successfully for Portfolio {PortfolioId}", portfolioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing resume for Portfolio {PortfolioId}", portfolioId);
                throw; // Re-throw so the consumer handler can NACK the message
            }
            finally
            {
                // Clean up uploaded file regardless of success/failure
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logger.LogDebug("Cleaned up uploaded file: {FilePath}", filePath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up file: {FilePath}", filePath);
                }
            }
        }
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync();
        if (_connection != null) await _connection.CloseAsync();
        await base.StopAsync(cancellationToken);
    }
}

// DTO Classes for JSON Parsing
public class AiResultDto
{
    public string Summary { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public List<ExperienceDto> Experience { get; set; } = new();
    public List<ProjectDto> Projects { get; set; } = new();
}

public class ExperienceDto
{
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    // Changed from 'Description' to 'Points' for structured bullet points
    public List<string> Points { get; set; } = new();
}

public class ProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string TechStack { get; set; } = string.Empty;
    // Changed from 'Description' to 'Points' for structured bullet points
    public List<string> Points { get; set; } = new();
}
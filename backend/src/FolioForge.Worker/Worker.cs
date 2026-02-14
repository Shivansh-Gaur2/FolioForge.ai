using FolioForge.Application.Common.Events;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FolioForge.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private IConnection _connection;
    private IChannel _channel;
    // My database is supposed to be scoped , so I need to create a scope to resolve it inside the worker
    // Instead of injecting the IPdfService directly, I inject the IServiceScopeFactory to create a scope when processing each message
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(queue: "resume_processing_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

        _logger.LogInformation(" [*] Waiting for messages. ");

        await base.StartAsync(cancellationToken);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation($" [x] Received raw JSON : {message}");

            try
            {
                var resumeEvent = JsonSerializer.Deserialize<ResumeUploadedEvent>(message);

                if (resumeEvent != null)
                {
                    _logger.LogInformation($"[BINGO] Processing resume for Portfolio {resumeEvent.PortfolioId}");
                    await ProcessResumeAsync(resumeEvent.FilePath, resumeEvent.PortfolioId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SNAP] Error Processing message : {ex.Message}");
            }
        };
        await _channel.BasicConsumeAsync(queue: "resume_processing_queue", autoAck: true, consumer: consumer);

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
                // 1. Extract & Analyze (Same as before)
                var text = pdfService.ExtractText(filePath);
                _logger.LogInformation($" ... Text extracted. Calling AI...");

                var jsonString = await aiService.GeneratePortfolioDataAsync(text);
                _logger.LogInformation(" ... AI Data Received!");
                _logger.LogInformation(jsonString);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<AiResultDto>(jsonString, options);

                // ==================================================
                // THE FIX: Transactional Nuke & Pave
                // ==================================================

                // 2. Fetch and Delete Old Sections
                var existingSections = await dbContext.Sections
                                            .Where(s => s.PortfolioId == portfolioId)
                                            .ToListAsync();

                if (existingSections.Any())
                {
                    dbContext.Sections.RemoveRange(existingSections);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation(" ... Old sections deleted.");
                }

                // 3. CRITICAL: Clear the Tracker
                // This tells EF Core: "Forget every object you are holding in memory."
                // This prevents it from accidentally trying to update the deleted rows.
                dbContext.ChangeTracker.Clear();

                // 4. Insert New Sections Directly
                // We don't need to fetch the Portfolio parent anymore. 
                // We just insert raw rows with the correct Foreign Key (PortfolioId).
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

                _logger.LogInformation(" ... ✅ DATABASE UPDATED SUCCESSFULLY!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SNAP] Error: {ex.Message}");
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
    public string Summary { get; set; }
    public List<string> Skills { get; set; }
    public List<ExperienceDto> Experience { get; set; }
    public List<ProjectDto> Projects { get; set; }
}

public class ExperienceDto
{
    public string Company { get; set; }
    public string Role { get; set; }
    public string Description { get; set; } // Or "Duration" depending on your prompt
}

public class ProjectDto
{
    public string Name { get; set; }
    public string TechStack { get; set; }
    public string Description { get; set; }
}
using System.Text;
using System.Text.Json;
using FolioForge.Application.Common.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FolioForge.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private IConnection _connection;
    private IChannel _channel;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
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
                    await ProcessResumeAsync(resumeEvent.FilePath);
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

    private async Task ProcessResumeAsync(string filePath)
    {
        _logger.LogInformation($" ... Reading file from: {filePath}");
        await Task.Delay(2000);
        _logger.LogInformation(" ... AI Analysis Complete!");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync();
        if (_connection != null) await _connection.CloseAsync();
        await base.StopAsync(cancellationToken);
    }
}



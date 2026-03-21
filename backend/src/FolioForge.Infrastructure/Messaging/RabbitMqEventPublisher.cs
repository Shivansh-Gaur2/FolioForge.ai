using System.Diagnostics;
using FolioForge.Domain.Interfaces;
using FolioForge.Infrastructure.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FolioForge.Infrastructure.Messaging
{
    /// <summary>
    /// Publishes domain events to RabbitMQ.
    /// 
    /// Design choices:
    /// ───────────────
    /// 1. A single persistent connection + channel is reused across publishes
    ///    (creating a new TCP connection per message is extremely expensive).
    /// 2. The queue is declared DURABLE so messages survive broker restarts.
    /// 3. Messages are marked PERSISTENT (DeliveryMode = DeliveryModes.Persistent)
    ///    so they are written to disk by the broker.
    /// 4. Connection/channel are lazily initialized and protected by a SemaphoreSlim
    ///    to allow async-safe initialization without blocking threads.
    /// </summary>
    public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
    {
        private readonly string _hostName;
        private readonly ILogger<RabbitMqEventPublisher> _logger;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        private IConnection? _connection;
        private IChannel? _channel;
        private bool _queueDeclared;

        private const string QueueName = "resume_processing_queue";

        public RabbitMqEventPublisher(IConfiguration config, ILogger<RabbitMqEventPublisher> logger)
        {
            _hostName = config["RabbitMq:HostName"] ?? "localhost";
            _logger = logger;
        }

        public async Task PublishAsync<T>(T @event) where T : class
        {
            // Start a Producer span — becomes a child of the current HTTP request span
            using var activity = FolioForgeDiagnostics.ActivitySource.StartActivity(
                FolioForgeDiagnostics.ProduceMessage,
                ActivityKind.Producer);

            // Tag with OTel Semantic Conventions for messaging
            activity?.SetTag(FolioForgeDiagnostics.Tags.MessagingSystem, "rabbitmq");
            activity?.SetTag(FolioForgeDiagnostics.Tags.MessagingDestination, QueueName);
            activity?.SetTag(FolioForgeDiagnostics.Tags.MessagingOperation, "publish");
            activity?.SetTag(FolioForgeDiagnostics.Tags.EventType, typeof(T).Name);

            var channel = await EnsureChannelAsync();

            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                // Mark message as persistent so RabbitMQ writes it to disk
                DeliveryMode = DeliveryModes.Persistent,
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString()
            };

            // ★ Inject trace context (TraceId + SpanId) into message headers
            // The Worker will extract this to continue the same trace
            RabbitMqContextPropagator.Inject(activity, props);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: QueueName,
                mandatory: false,
                basicProperties: props,
                body: body);

            // Record publish in custom metrics
            FolioForgeDiagnostics.MessagesPublished.Add(1,
                new KeyValuePair<string, object?>("event_type", typeof(T).Name),
                new KeyValuePair<string, object?>("success", "true"));
        }

        /// <summary>
        /// Lazily initializes a persistent connection + channel, and declares
        /// the durable queue once. Thread-safe via SemaphoreSlim.
        /// </summary>
        private async Task<IChannel> EnsureChannelAsync()
        {
            if (_channel is { IsOpen: true } && _queueDeclared)
                return _channel;

            await _initLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_channel is { IsOpen: true } && _queueDeclared)
                    return _channel;

                if (_connection is null || !_connection.IsOpen)
                {
                    var factory = new ConnectionFactory { HostName = _hostName };
                    _connection = await factory.CreateConnectionAsync();
                    _logger.LogInformation("RabbitMQ connection established to {Host}", _hostName);
                }

                _channel = await _connection.CreateChannelAsync();

                // Durable queue: survives broker restarts
                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _queueDeclared = true;
                return _channel;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
            }
            if (_connection != null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
            }
            _initLock.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

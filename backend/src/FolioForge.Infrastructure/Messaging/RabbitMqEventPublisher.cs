using FolioForge.Domain.Interfaces;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FolioForge.Infrastructure.Messaging
{
    public class RabbitMqEventPublisher : IEventPublisher
    {

        public async Task PublishAsync<T>(T @event) where T : class
        {
            // Here, you would implement the logic to publish the event to RabbitMQ.
            // This typically involves serializing the event and sending it to a specific exchange or queue.
            // For example:
            // 1. Create a connection to RabbitMQ.
            // 2. Create a channel.
            // 3. Declare an exchange or queue if necessary.
            // 4. Serialize the event (e.g., using JSON).
            // 5. Publish the message to the exchange or queue.
            // This is a placeholder implementation. You would replace this with actual RabbitMQ publishing logic.

            var factory = new ConnectionFactory() { HostName = "localhost" };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "resume_processing_queue",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties();

            await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "resume_processing_queue",
            mandatory: false,
            basicProperties: props,
            body: body);
        }
    }
}

using System.Text;
using System.Text.Json;
using Desafio.Features.Common.Messaging;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Desafio.Infrastructure.Azure;

/// <summary>
/// Local/development IMessageBus implementation backed by RabbitMQ.
/// Queue name convention: lowercase CLR type name.
/// </summary>
public sealed class RabbitMqMessageBus(
    string amqpConnectionString,
    ILogger<RabbitMqMessageBus> logger) : IMessageBus
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        ct.ThrowIfCancellationRequested();

        var queueName = typeof(T).Name.ToLowerInvariant();
        var factory = new ConnectionFactory
        {
            Uri = new Uri(amqpConnectionString),
            DispatchConsumersAsync = true
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = typeof(T).AssemblyQualifiedName;

        channel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: properties, body: body);

        logger.LogInformation("Published {EventType} to RabbitMQ queue '{QueueName}'", typeof(T).Name, queueName);
        return Task.CompletedTask;
    }
}

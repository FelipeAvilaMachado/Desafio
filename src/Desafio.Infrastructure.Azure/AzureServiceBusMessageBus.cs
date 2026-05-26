using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Desafio.Features.Common.Messaging;
using Microsoft.Extensions.Logging;

namespace Desafio.Infrastructure.Azure;

/// <summary>
/// IMessageBus implementation backed by Azure Service Bus.
/// Each message type maps to a topic named after the CLR type (lowercase).
/// </summary>
public sealed class AzureServiceBusMessageBus(
    ServiceBusClient client,
    ILogger<AzureServiceBusMessageBus> logger) : IMessageBus, IAsyncDisposable
{
    private readonly Dictionary<string, ServiceBusSender> _senders = [];

    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        var topicName = typeof(T).Name.ToLowerInvariant();

        if (!_senders.TryGetValue(topicName, out var sender))
        {
            sender = client.CreateSender(topicName);
            _senders[topicName] = sender;
        }

        var body = JsonSerializer.Serialize(message);
        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            Subject = typeof(T).Name
        };

        await sender.SendMessageAsync(sbMessage, ct);
        logger.LogInformation("Published {EventType} to Service Bus topic '{Topic}'", typeof(T).Name, topicName);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
            await sender.DisposeAsync();
    }
}

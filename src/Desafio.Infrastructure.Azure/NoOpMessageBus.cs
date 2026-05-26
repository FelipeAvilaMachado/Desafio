using Desafio.Features.Common.Messaging;
using Microsoft.Extensions.Logging;

namespace Desafio.Infrastructure.Azure;

/// <summary>
/// Development fallback when no valid Azure Service Bus connection string is configured.
/// </summary>
public sealed class NoOpMessageBus(ILogger<NoOpMessageBus> logger) : IMessageBus
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        logger.LogWarning("NoOpMessageBus active. Message of type {MessageType} was not published.", typeof(T).Name);
        return Task.CompletedTask;
    }
}
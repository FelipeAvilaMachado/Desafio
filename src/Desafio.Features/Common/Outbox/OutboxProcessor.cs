using System.Text.Json;
using Desafio.Features.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Desafio.Features.Common.Outbox;

/// <summary>
/// Background service that polls the outbox table every 5 seconds,
/// deserialises pending messages and publishes them to <see cref="IMessageBus"/>.
/// Guarantees at-least-once delivery; idempotency is the consumer's responsibility.
/// </summary>
public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "OutboxProcessor encountered an error during polling");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("OutboxProcessor stopped");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var pending = await outboxStore.GetPendingAsync(ct);
        if (pending.Count == 0) return;

        logger.LogInformation("OutboxProcessor: found {Count} pending message(s)", pending.Count);

        foreach (var message in pending)
        {
            try
            {
                var eventType = Type.GetType(message.EventType);
                if (eventType is null)
                {
                    logger.LogWarning("OutboxProcessor: unknown event type '{EventType}' — skipping", message.EventType);
                    continue;
                }

                var payload = JsonSerializer.Deserialize(message.Payload, eventType);
                if (payload is null)
                {
                    logger.LogWarning("OutboxProcessor: null payload for message {MessageId} — skipping", message.Id);
                    continue;
                }

                await bus.PublishAsync((dynamic)payload, ct);
                await outboxStore.MarkProcessedAsync(message.Id, ct);
                logger.LogInformation("OutboxProcessor: published message {MessageId} ({EventType})", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxProcessor: failed to publish message {MessageId}", message.Id);
                await outboxStore.MarkFailedAsync(message.Id, ex.Message, ct);
            }
        }
    }
}

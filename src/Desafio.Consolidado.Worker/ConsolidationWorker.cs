using System.Text;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Azure.Messaging.ServiceBus;
using Desafio.Features.Common.Entities;
using Desafio.Features.Common.Events;
using Desafio.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace Desafio.Consolidado.Worker;

/// <summary>
/// Background worker for consolidado processing.
/// Preferred mode is event-driven consumption from broker messages.
/// If no broker config is available, it falls back to periodic DB polling.
/// </summary>
public sealed class ConsolidationWorker(
    IServiceScopeFactory scopeFactory,
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IConnectionMultiplexer redis,
    ILogger<ConsolidationWorker> logger) : BackgroundService
{
    private const string EventQueueName = "lancamentocriadoevent";
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var messagingConnection = configuration.GetConnectionString("messaging")
            ?? configuration["Azure:ServiceBus:ConnectionString"];

        if (IsAmqpConnectionString(messagingConnection))
        {
            logger.LogInformation("ConsolidationWorker started in RabbitMQ mode");
            await ConsumeFromRabbitMqAsync(messagingConnection!, stoppingToken);
            return;
        }

        if (IsServiceBusConnectionString(messagingConnection))
        {
            logger.LogInformation("ConsolidationWorker started in Azure Service Bus mode");
            await ConsumeFromServiceBusAsync(messagingConnection!, stoppingToken);
            return;
        }

        var sqsQueueUrl = configuration[$"Aws:Sqs:Queues:{nameof(LancamentoCriadoEvent)}"]
            ?? configuration["Aws:Sqs:DefaultQueueUrl"];
        var sqsClient = serviceProvider.GetService<IAmazonSQS>();
        var ownsSqsClient = false;

        if (!string.IsNullOrWhiteSpace(sqsQueueUrl) && sqsClient is null)
        {
            sqsClient = new AmazonSQSClient();
            ownsSqsClient = true;
        }

        if (!string.IsNullOrWhiteSpace(sqsQueueUrl) && sqsClient is not null)
        {
            logger.LogInformation("ConsolidationWorker started in AWS SQS mode");
            try
            {
                await ConsumeFromSqsAsync(sqsClient, sqsQueueUrl, stoppingToken);
            }
            finally
            {
                if (ownsSqsClient)
                    sqsClient.Dispose();
            }

            return;
        }

        logger.LogWarning(
            "ConsolidationWorker started in POLLING fallback mode; no broker configuration was detected. Interval={Interval}s",
            PollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNewLancamentosAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "ConsolidationWorker: unhandled error during processing");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("ConsolidationWorker stopped (polling fallback)");
    }

    private async Task ConsumeFromRabbitMqAsync(string amqpConnectionString, CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(amqpConnectionString),
            DispatchConsumersAsync = true
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: EventQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 32, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, args) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(args.Body.ToArray());
                var @event = JsonSerializer.Deserialize<LancamentoCriadoEvent>(json);
                if (@event is null)
                {
                    logger.LogWarning("RabbitMQ message could not be deserialized to LancamentoCriadoEvent");
                    channel.BasicAck(args.DeliveryTag, multiple: false);
                    return;
                }

                await RecalculateForDateAsync(@event.Data, ct);
                channel.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "RabbitMQ processing failed; message will be requeued");
                channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _ = channel.BasicConsume(queue: EventQueueName, autoAck: false, consumer: consumer);
        logger.LogInformation("RabbitMQ consumer attached to queue '{QueueName}'", EventQueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("RabbitMQ consumer stopping");
        }
    }

    private async Task ConsumeFromServiceBusAsync(string connectionString, CancellationToken ct)
    {
        await using var client = new ServiceBusClient(connectionString);
        await using var processor = client.CreateProcessor(EventQueueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 8
        });

        processor.ProcessMessageAsync += async args =>
        {
            try
            {
                var @event = JsonSerializer.Deserialize<LancamentoCriadoEvent>(args.Message.Body.ToString());
                if (@event is null)
                {
                    logger.LogWarning("Service Bus message could not be deserialized to LancamentoCriadoEvent");
                    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                    return;
                }

                await RecalculateForDateAsync(@event.Data, args.CancellationToken);
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Service Bus processing failed; message will be abandoned");
                await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Service Bus processor error. Entity={Entity}", args.EntityPath);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(ct);
        logger.LogInformation("Service Bus processor started for queue '{QueueName}'", EventQueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Service Bus processor stopping");
        }
        finally
        {
            await processor.StopProcessingAsync(CancellationToken.None);
        }
    }

    private async Task ConsumeFromSqsAsync(IAmazonSQS sqsClient, string queueUrl, CancellationToken ct)
    {
        logger.LogInformation("SQS polling started for queue '{QueueUrl}'", queueUrl);

        while (!ct.IsCancellationRequested)
        {
            var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20,
                MessageAttributeNames = ["All"]
            }, ct);

            if (response.Messages.Count == 0)
                continue;

            foreach (var message in response.Messages)
            {
                try
                {
                    var @event = JsonSerializer.Deserialize<LancamentoCriadoEvent>(message.Body);
                    if (@event is null)
                    {
                        logger.LogWarning("SQS message could not be deserialized to LancamentoCriadoEvent");
                        await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, ct);
                        continue;
                    }

                    await RecalculateForDateAsync(@event.Data, ct);
                    await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "SQS processing failed; message will become visible again");
                }
            }
        }

        logger.LogInformation("SQS polling stopped");
    }

    private async Task ProcessNewLancamentosAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var writeDb = scope.ServiceProvider.GetRequiredService<ILancamentosDbContext>();

        // Find dates that have lancamentos but no up-to-date consolidado
        var pendingDates = await writeDb.Lancamentos
            .GroupBy(l => l.Data)
            .Select(g => g.Key)
            .ToListAsync(ct);

        if (pendingDates.Count == 0) return;

        logger.LogInformation("ConsolidationWorker: recalculating {Count} date(s)", pendingDates.Count);

        foreach (var date in pendingDates)
        {
            await RecalculateForDateAsync(date, ct);
        }
    }

    private async Task RecalculateForDateAsync(DateOnly date, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var readDb = scope.ServiceProvider.GetRequiredService<IConsolidadoReadDbContext>();
        var writeDb = scope.ServiceProvider.GetRequiredService<ILancamentosDbContext>();

        var lancamentos = await writeDb.Lancamentos
            .Where(l => l.Data == date)
            .ToListAsync(ct);

        var consolidado = ConsolidadoDiario.Calcular(date, lancamentos);
        await readDb.UpsertConsolidadoDiarioAsync(consolidado, ct);

        // Invalidate Redis cache for this date
        var cacheKey = $"consolidado:diario:{date:yyyy-MM-dd}";
        var cache = redis.GetDatabase();
        await cache.KeyDeleteAsync(cacheKey);

        logger.LogInformation(
            "ConsolidationWorker: consolidated {Date} — Debitos={Debitos}, Creditos={Creditos}, Saldo={Saldo}",
            date, consolidado.TotalDebitos, consolidado.TotalCreditos, consolidado.Saldo);
    }

    private static bool IsAmqpConnectionString(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
           (value.StartsWith("amqp://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("amqps://", StringComparison.OrdinalIgnoreCase));

    private static bool IsServiceBusConnectionString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            _ = ServiceBusConnectionStringProperties.Parse(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

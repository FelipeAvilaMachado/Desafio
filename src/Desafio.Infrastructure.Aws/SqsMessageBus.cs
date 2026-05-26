using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Desafio.Features.Common.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Desafio.Infrastructure.Aws;

/// <summary>
/// IMessageBus implementation backed by Amazon SQS.
/// Each message type maps to a queue URL from configuration key <c>Aws:Sqs:Queues:{TypeName}</c>.
/// Falls back to a default queue URL from <c>Aws:Sqs:DefaultQueueUrl</c>.
/// </summary>
public sealed class SqsMessageBus(
    IAmazonSQS sqsClient,
    IConfiguration configuration,
    ILogger<SqsMessageBus> logger) : IMessageBus
{
    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        var typeName = typeof(T).Name;
        var queueUrl = configuration[$"Aws:Sqs:Queues:{typeName}"]
            ?? configuration["Aws:Sqs:DefaultQueueUrl"];

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            logger.LogError("SqsMessageBus: no queue URL configured for {EventType}", typeName);
            throw new InvalidOperationException($"No SQS queue URL configured for event type '{typeName}'.");
        }

        var body = JsonSerializer.Serialize(message);
        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = body,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["EventType"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = typeName
                }
            }
        };

        var response = await sqsClient.SendMessageAsync(request, ct);
        logger.LogInformation(
            "Published {EventType} to SQS queue '{QueueUrl}' — MessageId: {MessageId}",
            typeName, queueUrl, response.MessageId);
    }
}

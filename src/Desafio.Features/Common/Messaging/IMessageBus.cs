namespace Desafio.Features.Common.Messaging;

/// <summary>
/// Abstraction over the message bus.
/// Azure implementation uses Service Bus; AWS implementation uses SQS.
/// Registered in the infrastructure layer — features depend only on this interface.
/// </summary>
public interface IMessageBus
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}

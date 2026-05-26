namespace Desafio.Features.Common.Outbox;

/// <summary>
/// Abstraction over the outbox persistence store.
/// Implemented in <c>Desafio.Infrastructure</c> using EF Core.
/// </summary>
public interface IOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(CancellationToken ct = default);
    Task MarkProcessedAsync(Guid id, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default);
    Task SaveAsync(OutboxMessage message, CancellationToken ct = default);
}

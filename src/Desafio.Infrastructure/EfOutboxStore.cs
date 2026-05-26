using Desafio.Features.Common.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Desafio.Infrastructure;

public sealed class EfOutboxStore(LancamentosDbContext db) : IOutboxStore
{
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(CancellationToken ct = default) =>
        await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Error == null)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

    public async Task MarkProcessedAsync(Guid id, CancellationToken ct = default)
    {
        var message = await db.OutboxMessages.FindAsync([id], ct);
        if (message is not null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default)
    {
        var message = await db.OutboxMessages.FindAsync([id], ct);
        if (message is not null)
        {
            message.Error = error;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task SaveAsync(OutboxMessage message, CancellationToken ct = default)
    {
        db.OutboxMessages.Add(message);
        // SaveChanges is called by the handler after adding the Lancamento
        await Task.CompletedTask;
    }
}

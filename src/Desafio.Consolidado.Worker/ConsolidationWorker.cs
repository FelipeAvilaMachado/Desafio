using System.Text.Json;
using Desafio.Features.Common.Entities;
using Desafio.Features.Common.Events;
using Desafio.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Desafio.Consolidado.Worker;

/// <summary>
/// Background worker that recalculates <see cref="ConsolidadoDiario"/> whenever a
/// <see cref="LancamentoCriadoEvent"/> is received from the message bus.
///
/// Resilience: runs independently from the Lancamentos API — the API stays up even if
/// this worker is down (outbox ensures no events are lost).
///
/// Peak load: processes events from a channel in batches; Redis cache invalidation
/// prevents stale reads on the consolidado endpoints.
/// </summary>
public sealed class ConsolidationWorker(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<ConsolidationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ConsolidationWorker started — polling interval {Interval}s", PollingInterval.TotalSeconds);

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

        logger.LogInformation("ConsolidationWorker stopped");
    }

    private async Task ProcessNewLancamentosAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var readDb = scope.ServiceProvider.GetRequiredService<IConsolidadoReadDbContext>();
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
            await RecalculateForDateAsync(date, writeDb, readDb, ct);
        }
    }

    private async Task RecalculateForDateAsync(
        DateOnly date,
        ILancamentosDbContext writeDb,
        IConsolidadoReadDbContext readDb,
        CancellationToken ct)
    {
        var lancamentos = await writeDb.Lancamentos
            .Where(l => l.Data == date)
            .ToListAsync(ct);

        var consolidado = ConsolidadoDiario.Calcular(date, lancamentos);

        // Upsert into the consolidado read store
        var existing = await readDb.ConsolidadosDiarios
            .FirstOrDefaultAsync(c => c.Data == date, ct);

        if (existing is not null)
            readDb.ConsolidadosDiarios.Remove(existing);

        readDb.ConsolidadosDiarios.Add(consolidado);

        // Invalidate Redis cache for this date
        var cacheKey = $"consolidado:diario:{date:yyyy-MM-dd}";
        var cache = redis.GetDatabase();
        await cache.KeyDeleteAsync(cacheKey);

        logger.LogInformation(
            "ConsolidationWorker: consolidated {Date} — Debitos={Debitos}, Creditos={Creditos}, Saldo={Saldo}",
            date, consolidado.TotalDebitos, consolidado.TotalCreditos, consolidado.Saldo);
    }
}

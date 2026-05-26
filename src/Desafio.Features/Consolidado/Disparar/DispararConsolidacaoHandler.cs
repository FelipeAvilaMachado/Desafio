using Desafio.Features.Common.Entities;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Desafio.Features.Consolidado.Disparar;

public sealed class DispararConsolidacaoHandler(
    ILancamentosDbContext writeDb,
    IConsolidadoReadDbContext readDb,
    IConnectionMultiplexer redis,
    ILogger<DispararConsolidacaoHandler> logger)
    : IHandler<DispararConsolidacaoCommand, DispararConsolidacaoResult>
{
    public async Task<DispararConsolidacaoResult> HandleAsync(DispararConsolidacaoCommand request, CancellationToken ct = default)
    {
        var query = writeDb.Lancamentos.AsNoTracking();

        if (request.DataInicio is not null)
            query = query.Where(l => l.Data >= request.DataInicio.Value);

        if (request.DataFim is not null)
            query = query.Where(l => l.Data <= request.DataFim.Value);

        var dates = await query
            .Select(l => l.Data)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(ct);

        if (dates.Count == 0)
            return new DispararConsolidacaoResult(0, [], 0m, 0m, 0m);

        var cache = redis.GetDatabase();
        decimal totalDebitos = 0m;
        decimal totalCreditos = 0m;

        foreach (var date in dates)
        {
            var lancamentos = await writeDb.Lancamentos
                .AsNoTracking()
                .Where(l => l.Data == date)
                .ToListAsync(ct);

            var consolidado = ConsolidadoDiario.Calcular(date, lancamentos);
            await readDb.UpsertConsolidadoDiarioAsync(consolidado, ct);
            await cache.KeyDeleteAsync($"consolidado:diario:{date:yyyy-MM-dd}");

            totalDebitos += consolidado.TotalDebitos;
            totalCreditos += consolidado.TotalCreditos;
        }

        logger.LogInformation(
            "Disparo manual de consolidacao processou {Count} data(s).",
            dates.Count);

        return new DispararConsolidacaoResult(
            dates.Count,
            dates,
            totalDebitos,
            totalCreditos,
            totalCreditos - totalDebitos);
    }
}

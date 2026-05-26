using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Desafio.Features.Consolidado.ObterPeriodo;

public sealed class ObterConsolidadoPeriodoHandler(
    IConsolidadoReadDbContext db,
    ILogger<ObterConsolidadoPeriodoHandler> logger)
    : IHandler<ObterConsolidadoPeriodoQuery, IReadOnlyList<ConsolidadoDiarioDto>>
{
    public async Task<IReadOnlyList<ConsolidadoDiarioDto>> HandleAsync(
        ObterConsolidadoPeriodoQuery request, CancellationToken ct = default)
    {
        if (request.DataFim < request.DataInicio)
            throw new ArgumentException("DataFim must be greater than or equal to DataInicio.");

        var result = await db.ConsolidadosDiarios
            .AsNoTracking()
            .Where(c => c.Data >= request.DataInicio && c.Data <= request.DataFim)
            .OrderBy(c => c.Data)
            .Select(c => ConsolidadoDiarioDto.FromEntity(c))
            .ToListAsync(ct);

        logger.LogInformation(
            "ObterConsolidadoPeriodo: {Count} dias entre {Inicio} e {Fim}",
            result.Count, request.DataInicio, request.DataFim);

        return result;
    }
}

using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Desafio.Features.Lancamentos.Listar;

public sealed class ListarLancamentosHandler(
    ILancamentosDbContext db,
    ILogger<ListarLancamentosHandler> logger)
    : IHandler<ListarLancamentosQuery, IReadOnlyList<LancamentoDto>>
{
    public async Task<IReadOnlyList<LancamentoDto>> HandleAsync(ListarLancamentosQuery request, CancellationToken ct = default)
    {
        var query = db.Lancamentos.AsNoTracking();

        if (request.Data.HasValue)
            query = query.Where(l => l.Data == request.Data.Value);

        var result = await query
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => LancamentoDto.FromEntity(l))
            .ToListAsync(ct);

        logger.LogInformation(
            "ListarLancamentos: retornados {Count} lançamentos{Filter}",
            result.Count,
            request.Data.HasValue ? $" para data {request.Data}" : string.Empty);

        return result;
    }
}

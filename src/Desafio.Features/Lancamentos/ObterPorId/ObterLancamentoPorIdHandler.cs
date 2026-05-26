using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Desafio.Features.Lancamentos.ObterPorId;

public sealed class ObterLancamentoPorIdHandler(
    ILancamentosDbContext db,
    ILogger<ObterLancamentoPorIdHandler> logger)
    : IHandler<ObterLancamentoPorIdQuery, LancamentoDto?>
{
    public async Task<LancamentoDto?> HandleAsync(ObterLancamentoPorIdQuery request, CancellationToken ct = default)
    {
        var lancamento = await db.Lancamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.Id, ct);

        if (lancamento is null)
        {
            logger.LogWarning("ObterLancamentoPorId: lançamento {Id} não encontrado", request.Id);
            return null;
        }

        logger.LogInformation("ObterLancamentoPorId: retornado lançamento {Id}", request.Id);
        return LancamentoDto.FromEntity(lancamento);
    }
}

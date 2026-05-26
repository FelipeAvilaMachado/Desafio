using System.Text.Json;
using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Entities;
using Desafio.Features.Common.Events;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Common.Outbox;
using Desafio.Features.Common.Persistence;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Desafio.Features.Lancamentos.Criar;

public sealed class CriarLancamentoHandler(
    ILancamentosDbContext db,
    IOutboxStore outboxStore,
    IValidator<CriarLancamentoCommand> validator,
    ILogger<CriarLancamentoHandler> logger)
    : IHandler<CriarLancamentoCommand, LancamentoDto>
{
    public async Task<LancamentoDto> HandleAsync(CriarLancamentoCommand request, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var tipo = Enum.Parse<TipoLancamento>(request.Tipo, ignoreCase: true);
        var lancamento = Lancamento.Criar(tipo, request.Valor, request.Descricao, request.Data);

        db.Lancamentos.Add(lancamento);

        var @event = new LancamentoCriadoEvent(
            lancamento.Id,
            lancamento.Tipo.ToString(),
            lancamento.Valor,
            lancamento.Descricao,
            lancamento.Data,
            DateTime.UtcNow);

        var outbox = new OutboxMessage
        {
            EventType = typeof(LancamentoCriadoEvent).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(@event)
        };
        await outboxStore.SaveAsync(outbox, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Lancamento {Id} criado — Tipo: {Tipo}, Valor: {Valor}, Data: {Data}",
            lancamento.Id, lancamento.Tipo, lancamento.Valor, lancamento.Data);

        return LancamentoDto.FromEntity(lancamento);
    }
}

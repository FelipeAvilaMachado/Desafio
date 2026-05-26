namespace Desafio.Features.Common.Events;

/// <summary>
/// Domain event raised when a <c>Lancamento</c> is created successfully.
/// Published via the outbox to decouple the write service from the consolidado worker.
/// </summary>
public sealed record LancamentoCriadoEvent(
    Guid LancamentoId,
    string Tipo,
    decimal Valor,
    string Descricao,
    DateOnly Data,
    DateTime OcorridoEm);

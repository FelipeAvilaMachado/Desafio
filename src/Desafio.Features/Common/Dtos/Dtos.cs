using Desafio.Features.Common.Entities;

namespace Desafio.Features.Common.Dtos;

public sealed record LancamentoDto(
    Guid Id,
    string Tipo,
    decimal Valor,
    string Descricao,
    DateOnly Data,
    DateTime CriadoEm)
{
    public static LancamentoDto FromEntity(Lancamento l) =>
        new(l.Id, l.Tipo.ToString(), l.Valor, l.Descricao, l.Data, l.CreatedAt);
}

public sealed record ConsolidadoDiarioDto(
    DateOnly Data,
    decimal TotalDebitos,
    decimal TotalCreditos,
    decimal Saldo,
    DateTime AtualizadoEm)
{
    public static ConsolidadoDiarioDto FromEntity(ConsolidadoDiario c) =>
        new(c.Data, c.TotalDebitos, c.TotalCreditos, c.Saldo, c.AtualizadoEm);
}

namespace Desafio.Features.Consolidado.Disparar;

public sealed record DispararConsolidacaoCommand(
    DateOnly? DataInicio,
    DateOnly? DataFim);

public sealed record DispararConsolidacaoResult(
    int DatasProcessadas,
    IReadOnlyList<DateOnly> Datas,
    decimal TotalDebitos,
    decimal TotalCreditos,
    decimal Saldo);

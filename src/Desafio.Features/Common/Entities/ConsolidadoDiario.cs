namespace Desafio.Features.Common.Entities;

public sealed class ConsolidadoDiario
{
    public Guid Id { get; private set; }
    public DateOnly Data { get; private set; }
    public decimal TotalDebitos { get; private set; }
    public decimal TotalCreditos { get; private set; }
    public decimal Saldo => TotalCreditos - TotalDebitos;
    public DateTime AtualizadoEm { get; private set; }

    private ConsolidadoDiario() { }

    public static ConsolidadoDiario Calcular(DateOnly data, IEnumerable<Lancamento> lancamentos)
    {
        var list = lancamentos.ToList();
        return new ConsolidadoDiario
        {
            Id = Guid.NewGuid(),
            Data = data,
            TotalDebitos = list.Where(l => l.Tipo == TipoLancamento.Debito).Sum(l => l.Valor),
            TotalCreditos = list.Where(l => l.Tipo == TipoLancamento.Credito).Sum(l => l.Valor),
            AtualizadoEm = DateTime.UtcNow
        };
    }
}

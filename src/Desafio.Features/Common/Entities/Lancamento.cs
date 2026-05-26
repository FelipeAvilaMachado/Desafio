namespace Desafio.Features.Common.Entities;

public enum TipoLancamento
{
    Debito = 1,
    Credito = 2
}

public sealed class Lancamento
{
    public Guid Id { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public decimal Valor { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public DateOnly Data { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Lancamento() { }

    public static Lancamento Criar(TipoLancamento tipo, decimal valor, string descricao, DateOnly data)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        return new Lancamento
        {
            Id = Guid.NewGuid(),
            Tipo = tipo,
            Valor = valor,
            Descricao = descricao,
            Data = data,
            CreatedAt = DateTime.UtcNow
        };
    }
}

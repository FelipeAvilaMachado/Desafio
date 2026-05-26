using Desafio.Features.Common.Entities;
using FluentAssertions;

namespace Desafio.Tests.Unit.Common;

public sealed class ConsolidadoDiarioTests
{
    [Fact]
    public void Calcular_MixedLancamentos_ComputesCorrectSaldo()
    {
        var data = DateOnly.FromDateTime(DateTime.Today);
        var lancamentos = new[]
        {
            Lancamento.Criar(TipoLancamento.Credito, 1000m, "Venda", data),
            Lancamento.Criar(TipoLancamento.Credito, 500m, "Servico", data),
            Lancamento.Criar(TipoLancamento.Debito, 200m, "Aluguel", data),
        };

        var result = ConsolidadoDiario.Calcular(data, lancamentos);

        result.TotalCreditos.Should().Be(1500m);
        result.TotalDebitos.Should().Be(200m);
        result.Saldo.Should().Be(1300m);
        result.Data.Should().Be(data);
    }

    [Fact]
    public void Calcular_NoLancamentos_ZeroSaldo()
    {
        var data = DateOnly.FromDateTime(DateTime.Today);

        var result = ConsolidadoDiario.Calcular(data, Enumerable.Empty<Lancamento>());

        result.TotalCreditos.Should().Be(0m);
        result.TotalDebitos.Should().Be(0m);
        result.Saldo.Should().Be(0m);
    }

    [Fact]
    public void Criar_NegativeValor_ThrowsArgumentException()
    {
        var act = () => Lancamento.Criar(TipoLancamento.Credito, -1m, "Negativo", DateOnly.FromDateTime(DateTime.Today));

        act.Should().Throw<ArgumentException>().WithMessage("*Valor*");
    }

    [Fact]
    public void Criar_EmptyDescricao_ThrowsArgumentException()
    {
        var act = () => Lancamento.Criar(TipoLancamento.Debito, 50m, "", DateOnly.FromDateTime(DateTime.Today));

        act.Should().Throw<ArgumentException>().WithMessage("*Descricao*");
    }
}

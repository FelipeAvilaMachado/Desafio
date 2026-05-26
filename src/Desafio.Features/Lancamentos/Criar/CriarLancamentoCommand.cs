using Desafio.Features.Common.Entities;
using FluentValidation;

namespace Desafio.Features.Lancamentos.Criar;

public sealed record CriarLancamentoCommand(
    string Tipo,
    decimal Valor,
    string Descricao,
    DateOnly Data);

public sealed class CriarLancamentoValidator : AbstractValidator<CriarLancamentoCommand>
{
    public CriarLancamentoValidator()
    {
        RuleFor(x => x.Tipo)
            .NotEmpty()
            .Must(t => Enum.TryParse<TipoLancamento>(t, ignoreCase: true, out _))
            .WithMessage("Tipo must be 'Debito' or 'Credito'.");

        RuleFor(x => x.Valor).GreaterThan(0).WithMessage("Valor must be greater than zero.");
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Data).NotEmpty();
    }
}

using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Desafio.Features.Lancamentos.Criar;

public static class CriarLancamentoServiceExtensions
{
    public static IServiceCollection AddCriarLancamento(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CriarLancamentoCommand>, CriarLancamentoValidator>();
        services.AddHandlerWithLogging<CriarLancamentoHandler, CriarLancamentoCommand, LancamentoDto>();
        return services;
    }
}

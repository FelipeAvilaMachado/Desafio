using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Desafio.Features.Lancamentos.ObterPorId;

public static class ObterLancamentoPorIdServiceExtensions
{
    public static IServiceCollection AddObterLancamentoPorId(this IServiceCollection services)
    {
        services.AddHandlerWithLogging<ObterLancamentoPorIdHandler, ObterLancamentoPorIdQuery, LancamentoDto?>();
        return services;
    }
}

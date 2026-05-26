using Desafio.Features.Common.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Desafio.Features.Consolidado.Disparar;

public static class DispararConsolidacaoServiceExtensions
{
    public static IServiceCollection AddDispararConsolidacao(this IServiceCollection services)
    {
        services.AddHandlerWithLogging<DispararConsolidacaoHandler, DispararConsolidacaoCommand, DispararConsolidacaoResult>();
        return services;
    }
}

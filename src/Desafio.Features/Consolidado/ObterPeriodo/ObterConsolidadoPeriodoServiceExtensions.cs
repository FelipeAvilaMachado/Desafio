using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Desafio.Features.Consolidado.ObterPeriodo;

public static class ObterConsolidadoPeriodoServiceExtensions
{
    public static IServiceCollection AddObterConsolidadoPeriodo(this IServiceCollection services)
    {
        services.AddHandlerWithLogging<ObterConsolidadoPeriodoHandler, ObterConsolidadoPeriodoQuery, IReadOnlyList<ConsolidadoDiarioDto>>();
        return services;
    }
}

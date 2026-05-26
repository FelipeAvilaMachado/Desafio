using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Desafio.Features.Consolidado.ObterDiario;

public static class ObterConsolidadoDiarioServiceExtensions
{
    public static IServiceCollection AddObterConsolidadoDiario(this IServiceCollection services)
    {
        services.AddHandlerWithLogging<ObterConsolidadoDiarioHandler, ObterConsolidadoDiarioQuery, ConsolidadoDiarioDto?>();
        return services;
    }
}

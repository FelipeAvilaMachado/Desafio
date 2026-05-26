using Desafio.Features.Common.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Desafio.Features.Benchmark.ExecutarCarga;

public static class ExecutarBenchmarkCargaServiceExtensions
{
    public static IServiceCollection AddExecutarBenchmarkCarga(this IServiceCollection services)
    {
        services.AddHandlerWithLogging<ExecutarBenchmarkCargaHandler, ExecutarBenchmarkCargaCommand, ExecutarBenchmarkCargaResult>();
        return services;
    }
}

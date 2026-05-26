using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Desafio.Features.Lancamentos.Listar;

public static class ListarLancamentosServiceExtensions
{
    public static IServiceCollection AddListarLancamentos(this IServiceCollection services)
    {
        services.AddHandlerWithLogging<ListarLancamentosHandler, ListarLancamentosQuery, IReadOnlyList<LancamentoDto>>();
        return services;
    }
}

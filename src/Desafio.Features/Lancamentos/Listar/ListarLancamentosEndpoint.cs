using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Desafio.Features.Lancamentos.Listar;

public static class ListarLancamentosEndpoint
{
    public static IEndpointRouteBuilder MapListarLancamentos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/lancamentos", async (
            DateOnly? data,
            IHandler<ListarLancamentosQuery, IReadOnlyList<LancamentoDto>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new ListarLancamentosQuery(data), ct);
            return Results.Ok(result);
        })
        .WithName("ListarLancamentos")
        .WithSummary("Lista lançamentos, com filtro opcional por data")
        .WithTags("Lancamentos")
        .Produces<IReadOnlyList<LancamentoDto>>();

        return app;
    }
}

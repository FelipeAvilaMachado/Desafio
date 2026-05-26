using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Desafio.Features.Lancamentos.ObterPorId;

public static class ObterLancamentoPorIdEndpoint
{
    public static IEndpointRouteBuilder MapObterLancamentoPorId(this IEndpointRouteBuilder app)
    {
        app.MapGet("/lancamentos/{id:guid}", async (
            Guid id,
            IHandler<ObterLancamentoPorIdQuery, LancamentoDto?> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new ObterLancamentoPorIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("ObterLancamentoPorId")
        .WithSummary("Obtém um lançamento pelo seu identificador")
        .WithTags("Lancamentos")
        .Produces<LancamentoDto>()
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}

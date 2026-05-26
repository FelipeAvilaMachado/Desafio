using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Desafio.Features.Consolidado.ObterDiario;

public static class ObterConsolidadoDiarioEndpoint
{
    public static IEndpointRouteBuilder MapObterConsolidadoDiario(this IEndpointRouteBuilder app)
    {
        app.MapGet("/consolidado/diario", async (
            DateOnly data,
            IHandler<ObterConsolidadoDiarioQuery, ConsolidadoDiarioDto?> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new ObterConsolidadoDiarioQuery(data), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("ObterConsolidadoDiario")
        .WithSummary("Retorna o saldo consolidado de um dia específico")
        .WithTags("Consolidado")
        .Produces<ConsolidadoDiarioDto>()
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}

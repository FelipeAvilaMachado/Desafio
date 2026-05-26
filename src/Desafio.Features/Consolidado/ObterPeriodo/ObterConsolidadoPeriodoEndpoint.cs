using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Desafio.Features.Consolidado.ObterPeriodo;

public static class ObterConsolidadoPeriodoEndpoint
{
    public static IEndpointRouteBuilder MapObterConsolidadoPeriodo(this IEndpointRouteBuilder app)
    {
        app.MapGet("/consolidado/periodo", async (
            DateOnly dataInicio,
            DateOnly dataFim,
            IHandler<ObterConsolidadoPeriodoQuery, IReadOnlyList<ConsolidadoDiarioDto>> handler,
            CancellationToken ct) =>
        {
            if (dataFim < dataInicio)
                return Results.BadRequest(new { error = "dataFim must be >= dataInicio" });

            var result = await handler.HandleAsync(new ObterConsolidadoPeriodoQuery(dataInicio, dataFim), ct);
            return Results.Ok(result);
        })
        .WithName("ObterConsolidadoPeriodo")
        .WithSummary("Retorna o consolidado diário para um período (semanal ou mensal)")
        .WithTags("Consolidado")
        .Produces<IReadOnlyList<ConsolidadoDiarioDto>>();

        return app;
    }
}

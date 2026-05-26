using Desafio.Features.Common.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Desafio.Features.Consolidado.Disparar;

public static class DispararConsolidacaoEndpoint
{
    public static IEndpointRouteBuilder MapDispararConsolidacao(this IEndpointRouteBuilder app)
    {
        app.MapPost("/consolidado/disparar", async (
            DispararConsolidacaoCommand command,
            IHandler<DispararConsolidacaoCommand, DispararConsolidacaoResult> handler,
            CancellationToken ct) =>
        {
            if ((command.DataInicio is null) != (command.DataFim is null))
                return Results.BadRequest(new { error = "Forneca ambos DataInicio e DataFim, ou nenhum." });

            if (command.DataInicio is not null && command.DataFim is not null && command.DataFim < command.DataInicio)
                return Results.BadRequest(new { error = "DataFim must be >= DataInicio" });

            var result = await handler.HandleAsync(command, ct);
            return Results.Ok(result);
        })
        .WithName("DispararConsolidacao")
        .WithSummary("Dispara a consolidacao manualmente para um periodo ou para todas as datas")
        .WithTags("Consolidado")
        .Produces<DispararConsolidacaoResult>()
        .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}

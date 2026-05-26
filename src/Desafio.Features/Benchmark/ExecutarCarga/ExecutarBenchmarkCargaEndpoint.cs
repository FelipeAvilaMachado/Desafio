using Desafio.Features.Common.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Desafio.Features.Benchmark.ExecutarCarga;

public static class ExecutarBenchmarkCargaEndpoint
{
    public static IEndpointRouteBuilder MapExecutarBenchmarkCarga(this IEndpointRouteBuilder app)
    {
        app.MapPost("/benchmark/carga", async (
            ExecutarBenchmarkCargaCommand command,
            IHandler<ExecutarBenchmarkCargaCommand, ExecutarBenchmarkCargaResult> handler,
            CancellationToken ct) =>
        {
            if (command.Total is < 1 or > 50_000)
                return Results.BadRequest(new { error = "Total must be between 1 and 50000." });

            if (command.Concorrencia is < 1 or > 512)
                return Results.BadRequest(new { error = "Concorrencia must be between 1 and 512." });

            var result = await handler.HandleAsync(command, ct);
            return Results.Ok(result);
        })
        .WithName("ExecutarBenchmarkCarga")
        .WithSummary("Executa benchmark de criação de lançamentos no lado do servidor com percentis de latência")
        .WithTags("Benchmark")
        .Produces<ExecutarBenchmarkCargaResult>()
        .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}

using System.Net;
using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Consolidado.ObterDiario;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Desafio.Functions.Azure.Consolidado;

public sealed class ObterConsolidadoDiarioAzureFunction(
    IHandler<ObterConsolidadoDiarioQuery, ConsolidadoDiarioDto?> handler,
    ILogger<ObterConsolidadoDiarioAzureFunction> logger)
{
    [Function("ObterConsolidadoDiario")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "consolidado/diario")] HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation("Azure Function ObterConsolidadoDiario triggered");

        if (req.Query["data"] is not { } raw || !DateOnly.TryParse(raw, out var data))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { error = "Query parameter 'data' (yyyy-MM-dd) is required." }, ct);
            return bad;
        }

        var result = await handler.HandleAsync(new ObterConsolidadoDiarioQuery(data), ct);
        if (result is null)
            return req.CreateResponse(HttpStatusCode.NotFound);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result, ct);
        return response;
    }
}

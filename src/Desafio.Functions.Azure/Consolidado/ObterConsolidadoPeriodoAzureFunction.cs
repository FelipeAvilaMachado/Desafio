using System.Net;
using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Consolidado.ObterPeriodo;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Desafio.Functions.Azure.Consolidado;

public sealed class ObterConsolidadoPeriodoAzureFunction(
    IHandler<ObterConsolidadoPeriodoQuery, IReadOnlyList<ConsolidadoDiarioDto>> handler,
    ILogger<ObterConsolidadoPeriodoAzureFunction> logger)
{
    [Function("ObterConsolidadoPeriodo")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "consolidado/periodo")] HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation("Azure Function ObterConsolidadoPeriodo triggered");

        if (req.Query["dataInicio"] is not { } rawInicio || !DateOnly.TryParse(rawInicio, out var dataInicio) ||
            req.Query["dataFim"] is not { } rawFim || !DateOnly.TryParse(rawFim, out var dataFim))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { error = "Query parameters 'dataInicio' and 'dataFim' (yyyy-MM-dd) are required." }, ct);
            return bad;
        }

        if (dataFim < dataInicio)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { error = "dataFim must be >= dataInicio." }, ct);
            return bad;
        }

        var result = await handler.HandleAsync(new ObterConsolidadoPeriodoQuery(dataInicio, dataFim), ct);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result, ct);
        return response;
    }
}

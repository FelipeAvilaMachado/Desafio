using System.Net;
using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Lancamentos.ObterPorId;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Desafio.Functions.Azure.Lancamentos;

public sealed class ObterLancamentoPorIdAzureFunction(
    IHandler<ObterLancamentoPorIdQuery, LancamentoDto?> handler,
    ILogger<ObterLancamentoPorIdAzureFunction> logger)
{
    [Function("ObterLancamentoPorId")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "lancamentos/{id:guid}")] HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        logger.LogInformation("Azure Function ObterLancamentoPorId triggered for {Id}", id);

        var result = await handler.HandleAsync(new ObterLancamentoPorIdQuery(id), ct);
        if (result is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result, ct);
        return response;
    }
}

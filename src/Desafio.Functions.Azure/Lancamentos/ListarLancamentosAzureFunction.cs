using System.Net;
using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Lancamentos.Listar;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Desafio.Functions.Azure.Lancamentos;

public sealed class ListarLancamentosAzureFunction(
    IHandler<ListarLancamentosQuery, IReadOnlyList<LancamentoDto>> handler,
    ILogger<ListarLancamentosAzureFunction> logger)
{
    [Function("ListarLancamentos")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "lancamentos")] HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation("Azure Function ListarLancamentos triggered");

        DateOnly? data = null;
        if (req.Query["data"] is { } raw && DateOnly.TryParse(raw, out var parsed))
            data = parsed;

        var result = await handler.HandleAsync(new ListarLancamentosQuery(data), ct);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result, ct);
        return response;
    }
}

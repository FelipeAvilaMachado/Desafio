using System.Net;
using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Lancamentos.Criar;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Desafio.Functions.Azure.Lancamentos;

public sealed class CriarLancamentoAzureFunction(
    IHandler<CriarLancamentoCommand, LancamentoDto> handler,
    ILogger<CriarLancamentoAzureFunction> logger)
{
    [Function("CriarLancamento")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "lancamentos")] HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation("Azure Function CriarLancamento triggered");

        var command = await req.ReadFromJsonAsync<CriarLancamentoCommand>(ct);
        if (command is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { error = "Invalid request body." }, ct);
            return bad;
        }

        try
        {
            var result = await handler.HandleAsync(command, ct);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result, ct);
            return response;
        }
        catch (ValidationException ex)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(new { errors = ex.Errors.Select(e => e.ErrorMessage) }, ct);
            return response;
        }
    }
}

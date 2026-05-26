using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Desafio.Features.Lancamentos.Criar;

public static class CriarLancamentoEndpoint
{
    public static IEndpointRouteBuilder MapCriarLancamento(this IEndpointRouteBuilder app)
    {
        app.MapPost("/lancamentos", async (
            CriarLancamentoCommand command,
            IHandler<CriarLancamentoCommand, LancamentoDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(command, ct);
                return Results.Created($"/api/lancamentos/{result.Id}", result);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(
                    ex.Errors.GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }
        })
        .WithName("CriarLancamento")
        .WithSummary("Registra um novo lançamento (débito ou crédito)")
        .WithTags("Lancamentos")
        .Produces<LancamentoDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        return app;
    }
}

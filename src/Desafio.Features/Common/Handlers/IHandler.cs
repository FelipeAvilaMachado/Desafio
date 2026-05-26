namespace Desafio.Features.Common.Handlers;

/// <summary>
/// Core abstraction for all vertical slice handlers.
/// Replaces MediatR — each feature owns a concrete implementation.
/// </summary>
public interface IHandler<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken ct = default);
}

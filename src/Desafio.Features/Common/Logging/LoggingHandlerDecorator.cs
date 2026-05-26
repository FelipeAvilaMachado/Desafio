using System.Diagnostics;
using Desafio.Features.Common.Handlers;
using Microsoft.Extensions.Logging;

namespace Desafio.Features.Common.Logging;

/// <summary>
/// Decorator that wraps any <see cref="IHandler{TRequest,TResponse}"/> and emits
/// structured log entries for every request: name, elapsed time, and exceptions.
/// Registered automatically by <c>AddHandlerWithLogging</c> — callers never reference it directly.
/// </summary>
public sealed class LoggingHandlerDecorator<TRequest, TResponse>(
    IHandler<TRequest, TResponse> inner,
    ILogger<LoggingHandlerDecorator<TRequest, TResponse>> logger)
    : IHandler<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken ct = default)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await inner.HandleAsync(request, ct);
            sw.Stop();
            logger.LogInformation(
                "Handled {RequestName} successfully in {ElapsedMs}ms",
                name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(
                ex,
                "Error handling {RequestName} after {ElapsedMs}ms",
                name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

using Desafio.Features.Common.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Desafio.Features.Common.Handlers;

public static class HandlerServiceExtensions
{
    /// <summary>
    /// Registers <typeparamref name="THandler"/> as the concrete implementation of
    /// <see cref="IHandler{TRequest,TResponse}"/> and wraps it with
    /// <see cref="LoggingHandlerDecorator{TRequest,TResponse}"/> transparently.
    /// </summary>
    public static IServiceCollection AddHandlerWithLogging<THandler, TRequest, TResponse>(
        this IServiceCollection services)
        where THandler : class, IHandler<TRequest, TResponse>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IHandler<TRequest, TResponse>>(sp =>
            new LoggingHandlerDecorator<TRequest, TResponse>(
                sp.GetRequiredService<THandler>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LoggingHandlerDecorator<TRequest, TResponse>>>()));
        return services;
    }
}

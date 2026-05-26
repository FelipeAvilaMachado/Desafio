using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Desafio.Features.Common.Auth;

/// <summary>
/// Mock for Amazon API Gateway API key validation.
/// Reads the expected key from configuration key <c>Auth:ApiGateway:ApiKey</c>.
/// Returns 403 when the <c>x-api-key</c> header is missing or invalid.
/// </summary>
public sealed class MockApiGatewayMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<MockApiGatewayMiddleware> logger)
{
    private const string HeaderName = "x-api-key";
    private const string ConfigKey = "Auth:ApiGateway:ApiKey";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/alive"))
        {
            await next(context);
            return;
        }

        var expectedKey = configuration[ConfigKey];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            logger.LogWarning(
                "MockApiGatewayMiddleware: config key '{ConfigKey}' is not set — bypassing API key check in development",
                ConfigKey);
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var headerValue) ||
            !headerValue.ToString().Equals(expectedKey, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "MockApiGatewayMiddleware: invalid or missing API key from {RemoteIp}",
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Forbidden", detail = "Invalid or missing API Gateway key." });
            return;
        }

        logger.LogDebug("MockApiGatewayMiddleware: API key validated for {RemoteIp}", context.Connection.RemoteIpAddress);
        await next(context);
    }
}

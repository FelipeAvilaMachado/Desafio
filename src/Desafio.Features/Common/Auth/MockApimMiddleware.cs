using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Desafio.Features.Common.Auth;

/// <summary>
/// Mock for Azure API Management subscription-key validation.
/// Reads the expected key from configuration key <c>Auth:Apim:SubscriptionKey</c>.
/// Returns 403 when the <c>Ocp-Apim-Subscription-Key</c> header is missing or invalid.
/// </summary>
public sealed class MockApimMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<MockApimMiddleware> logger)
{
    private const string HeaderName = "Ocp-Apim-Subscription-Key";
    private const string ConfigKey = "Auth:Apim:SubscriptionKey";

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
                "MockApimMiddleware: config key '{ConfigKey}' is not set — bypassing APIM check in development",
                ConfigKey);
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var headerValue) ||
            !headerValue.ToString().Equals(expectedKey, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "MockApimMiddleware: invalid or missing subscription key from {RemoteIp}",
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Forbidden", detail = "Invalid or missing APIM subscription key." });
            return;
        }

        logger.LogDebug("MockApimMiddleware: subscription key validated for {RemoteIp}", context.Connection.RemoteIpAddress);
        await next(context);
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Desafio.Features.Common.Auth;

/// <summary>
/// Mock for Microsoft Entra ID (Azure AD) token validation.
/// Reads the expected token from configuration key <c>Auth:EntraId:ValidToken</c>.
/// Returns 401 when the Authorization header is missing or does not match.
/// </summary>
public sealed class MockEntraIdMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<MockEntraIdMiddleware> logger)
{
    private const string HeaderName = "Authorization";
    private const string ConfigKey = "Auth:EntraId:ValidToken";

    public async Task InvokeAsync(HttpContext context)
    {
        // Health-check endpoints bypass auth
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/alive"))
        {
            await next(context);
            return;
        }

        var expectedToken = configuration[ConfigKey];
        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            logger.LogWarning(
                "MockEntraIdMiddleware: config key '{ConfigKey}' is not set — bypassing auth check in development",
                ConfigKey);
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var headerValue) ||
            !headerValue.ToString().Equals($"Bearer {expectedToken}", StringComparison.Ordinal))
        {
            logger.LogWarning(
                "MockEntraIdMiddleware: invalid or missing Bearer token from {RemoteIp}",
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized", detail = "Invalid or missing Entra ID token." });
            return;
        }

        logger.LogDebug("MockEntraIdMiddleware: token validated for {RemoteIp}", context.Connection.RemoteIpAddress);
        await next(context);
    }
}

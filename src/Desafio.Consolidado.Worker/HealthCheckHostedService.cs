using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Desafio.Consolidado.Worker;

/// <summary>
/// Exposes a /health HTTP endpoint on port 8081 so Aspire and container orchestrators
/// can probe the worker independently of the Lancamentos API.
/// </summary>
public sealed class HealthCheckHostedService(
    HealthCheckService healthCheckService,
    ILogger<HealthCheckHostedService> logger) : IHostedService
{
    private HttpListener? _listener;

    public Task StartAsync(CancellationToken ct)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://+:8081/health/");
        _listener.Start();
        logger.LogInformation("HealthCheckHostedService listening on http://+:8081/health/");
        _ = AcceptLoopAsync(ct);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _listener?.Stop();
        return Task.CompletedTask;
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && (_listener?.IsListening == true))
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                var report = await healthCheckService.CheckHealthAsync(ct);
                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
                var json = JsonSerializer.Serialize(new { status = report.Status.ToString() });
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                await ctx.Response.OutputStream.WriteAsync(bytes, ct);
                ctx.Response.Close();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "HealthCheckHostedService error");
            }
        }
    }
}

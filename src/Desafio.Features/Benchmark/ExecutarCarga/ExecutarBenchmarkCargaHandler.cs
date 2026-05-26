using System.Collections.Concurrent;
using System.Diagnostics;
using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Lancamentos.Criar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Desafio.Features.Benchmark.ExecutarCarga;

public sealed class ExecutarBenchmarkCargaHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<ExecutarBenchmarkCargaHandler> logger)
    : IHandler<ExecutarBenchmarkCargaCommand, ExecutarBenchmarkCargaResult>
{
    public async Task<ExecutarBenchmarkCargaResult> HandleAsync(ExecutarBenchmarkCargaCommand request, CancellationToken ct = default)
    {
        var total = Math.Clamp(request.Total, 1, 50_000);
        var concorrencia = Math.Clamp(request.Concorrencia, 1, 512);
        var dataBase = request.DataBase ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var latencias = new ConcurrentBag<double>();
        var errorSamples = new ConcurrentQueue<string>();

        var cursor = 0;
        var success = 0;
        var failed = 0;

        var startedAt = Stopwatch.GetTimestamp();

        async Task Worker()
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var criarLancamentoHandler = scope.ServiceProvider
                .GetRequiredService<IHandler<CriarLancamentoCommand, LancamentoDto>>();

            while (!ct.IsCancellationRequested)
            {
                var index = Interlocked.Increment(ref cursor) - 1;
                if (index >= total)
                    break;

                var data = dataBase.AddDays(-(index % 5));
                var tipo = index % 2 == 0 ? "Credito" : "Debito";
                var valor = decimal.Round(50m + (index % 1000) * 0.17m, 2);
                var descricao = $"Benchmark server #{index + 1}";

                var opStart = Stopwatch.GetTimestamp();
                try
                {
                    await criarLancamentoHandler.HandleAsync(
                        new CriarLancamentoCommand(tipo, valor, descricao, data),
                        ct);

                    Interlocked.Increment(ref success);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    if (errorSamples.Count < 10)
                        errorSamples.Enqueue($"{ex.GetType().Name}: {ex.Message}");
                }
                finally
                {
                    latencias.Add(Stopwatch.GetElapsedTime(opStart).TotalMilliseconds);
                }
            }
        }

        await Task.WhenAll(Enumerable.Range(0, concorrencia).Select(_ => Worker()));

        var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        var reqPerSecond = elapsedMs <= 0 ? success : (success * 1000d) / elapsedMs;

        var ordered = latencias.OrderBy(v => v).ToArray();
        var p50 = Percentile(ordered, 0.50);
        var p95 = Percentile(ordered, 0.95);
        var p99 = Percentile(ordered, 0.99);

        logger.LogInformation(
            "Benchmark server-side concluido: Total={Total}, Success={Success}, Failed={Failed}, RPS={Rps}",
            total, success, failed, reqPerSecond);

        return new ExecutarBenchmarkCargaResult(
            total,
            success,
            failed,
            elapsedMs,
            reqPerSecond,
            p50,
            p95,
            p99,
            errorSamples.ToArray());
    }

    private static double Percentile(double[] values, double percentile)
    {
        if (values.Length == 0)
            return 0;

        var rank = (int)Math.Ceiling(percentile * values.Length) - 1;
        var index = Math.Clamp(rank, 0, values.Length - 1);
        return values[index];
    }
}

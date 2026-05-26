namespace Desafio.Features.Benchmark.ExecutarCarga;

public sealed record ExecutarBenchmarkCargaCommand(
    int Total = 1000,
    int Concorrencia = 64,
    DateOnly? DataBase = null);

public sealed record ExecutarBenchmarkCargaResult(
    int Total,
    int Success,
    int Failed,
    double ElapsedMs,
    double ReqPerSecond,
    double P50Ms,
    double P95Ms,
    double P99Ms,
    IReadOnlyList<string> ErrorSamples);

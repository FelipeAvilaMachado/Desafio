using Desafio.Consolidado.Worker;
using Desafio.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Register EF Core (write + read contexts), Redis, IOutboxStore
builder.Services.AddInfrastructure(builder.Configuration);

// The consolidation background worker
builder.Services.AddHostedService<ConsolidationWorker>();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// Expose health endpoint on a dedicated port for Aspire / container probes
builder.Services.AddHostedService<HealthCheckHostedService>();

var host = builder.Build();
host.Run();

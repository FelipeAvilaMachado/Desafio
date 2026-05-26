using Desafio.Consolidado.Worker;
using Desafio.Infrastructure;
using Microsoft.EntityFrameworkCore;

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
await EnsureDatabasesCreatedAsync(host.Services, host.Services.GetRequiredService<IHostEnvironment>(), host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
host.Run();

static async Task EnsureDatabasesCreatedAsync(
    IServiceProvider services,
    IHostEnvironment environment,
    CancellationToken ct)
{
    await using var scope = services.CreateAsyncScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInit");
    var writeDb = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
    var readDb = scope.ServiceProvider.GetRequiredService<ConsolidadoReadDbContext>();

    const int maxAttempts = 20;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await writeDb.Database.EnsureCreatedAsync(ct);
            await EnsureConsolidadoSchemaAsync(readDb, ct);
            logger.LogInformation("Database initialization completed for {EnvironmentName}.", environment.EnvironmentName);
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Database initialization attempt {Attempt}/{MaxAttempts} failed; retrying...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }
}

static Task EnsureConsolidadoSchemaAsync(ConsolidadoReadDbContext readDb, CancellationToken ct)
{
    return readDb.Database.ExecuteSqlRawAsync(
        """
        IF OBJECT_ID(N'[dbo].[ConsolidadosDiarios]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[ConsolidadosDiarios](
                [Id] uniqueidentifier NOT NULL,
                [Data] date NOT NULL,
                [TotalDebitos] decimal(18,2) NOT NULL,
                [TotalCreditos] decimal(18,2) NOT NULL,
                [AtualizadoEm] datetime2 NOT NULL,
                CONSTRAINT [PK_ConsolidadosDiarios] PRIMARY KEY ([Id])
            );
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = N'IX_ConsolidadosDiarios_Data'
              AND object_id = OBJECT_ID(N'[dbo].[ConsolidadosDiarios]'))
        BEGIN
            CREATE UNIQUE INDEX [IX_ConsolidadosDiarios_Data]
            ON [dbo].[ConsolidadosDiarios] ([Data]);
        END;
        """,
        ct);
}

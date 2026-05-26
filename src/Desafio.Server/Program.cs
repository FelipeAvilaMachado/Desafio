using Desafio.Features.Common.Auth;
using Desafio.Features.Benchmark.ExecutarCarga;
using Desafio.Features.Consolidado.Disparar;
using Desafio.Features.Consolidado.ObterDiario;
using Desafio.Features.Consolidado.ObterPeriodo;
using Desafio.Features.Common.Outbox;
using Desafio.Features.Lancamentos.Criar;
using Desafio.Features.Lancamentos.Listar;
using Desafio.Features.Lancamentos.ObterPorId;
using Desafio.Infrastructure;
using Desafio.Infrastructure.Azure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, service discovery
builder.AddServiceDefaults();
builder.AddRedisClientBuilder("cache").WithOutputCache();

// OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Vertical slice handlers (each slice registers its own IHandler + logging decorator)
builder.Services
    .AddCriarLancamento()
    .AddExecutarBenchmarkCarga()
    .AddListarLancamentos()
    .AddObterLancamentoPorId()
    .AddDispararConsolidacao()
    .AddObterConsolidadoDiario()
    .AddObterConsolidadoPeriodo();

// Infrastructure: EF Core (single SQL Server database), Redis IConnectionMultiplexer, IOutboxStore
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxProcessor>();

// Azure-specific: Service Bus (IMessageBus) + Key Vault
builder.Services.AddAzureInfrastructure(builder.Configuration);

var app = builder.Build();

await EnsureDatabasesCreatedAsync(app.Services, app.Logger, app.Lifetime.ApplicationStopping);

// Mock auth middlewares — skip when config keys are absent (local dev)
app.UseMiddleware<MockApimMiddleware>();
app.UseMiddleware<MockEntraIdMiddleware>();

app.UseExceptionHandler();
app.UseOutputCache();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// All business endpoints under /api
var api = app.MapGroup("/api");
api.MapCriarLancamento();
api.MapExecutarBenchmarkCarga();
api.MapListarLancamentos();
api.MapObterLancamentoPorId();
api.MapDispararConsolidacao();
api.MapObterConsolidadoDiario();
api.MapObterConsolidadoPeriodo();

// Aspire health + liveness endpoints
app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

static async Task EnsureDatabasesCreatedAsync(
    IServiceProvider services,
    ILogger logger,
    CancellationToken ct)
{
    await using var scope = services.CreateAsyncScope();
    var writeDb = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
    var readDb = scope.ServiceProvider.GetRequiredService<ConsolidadoReadDbContext>();

    const int maxAttempts = 20;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await writeDb.Database.EnsureCreatedAsync(ct);
            await EnsureConsolidadoSchemaAsync(readDb, ct);
            logger.LogInformation("Database initialization completed successfully.");
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

// Needed for WebApplicationFactory<Program> in integration tests
public partial class Program;

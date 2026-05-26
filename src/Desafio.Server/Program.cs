using Desafio.Features.Common.Auth;
using Desafio.Features.Consolidado.ObterDiario;
using Desafio.Features.Consolidado.ObterPeriodo;
using Desafio.Features.Lancamentos.Criar;
using Desafio.Features.Lancamentos.Listar;
using Desafio.Features.Lancamentos.ObterPorId;
using Desafio.Infrastructure;
using Desafio.Infrastructure.Azure;

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
    .AddListarLancamentos()
    .AddObterLancamentoPorId()
    .AddObterConsolidadoDiario()
    .AddObterConsolidadoPeriodo();

// Infrastructure: EF Core (SQL Server write + read replica), Redis IConnectionMultiplexer, IOutboxStore
builder.Services.AddInfrastructure(builder.Configuration);

// Azure-specific: Service Bus (IMessageBus) + Key Vault
builder.Services.AddAzureInfrastructure(builder.Configuration);

var app = builder.Build();

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
api.MapListarLancamentos();
api.MapObterLancamentoPorId();
api.MapObterConsolidadoDiario();
api.MapObterConsolidadoPeriodo();

// Aspire health + liveness endpoints
app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

// Needed for WebApplicationFactory<Program> in integration tests
public partial class Program;

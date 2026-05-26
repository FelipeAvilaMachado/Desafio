using Desafio.Features.Consolidado.ObterDiario;
using Desafio.Features.Consolidado.ObterPeriodo;
using Desafio.Features.Common.Outbox;
using Desafio.Features.Lancamentos.Criar;
using Desafio.Features.Lancamentos.Listar;
using Desafio.Features.Lancamentos.ObterPorId;
using Desafio.Infrastructure;
using Desafio.Infrastructure.Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights
builder.Services.AddApplicationInsightsTelemetryWorkerService();

// Vertical slice handlers (same registrations as Desafio.Server)
builder.Services
    .AddCriarLancamento()
    .AddListarLancamentos()
    .AddObterLancamentoPorId()
    .AddObterConsolidadoDiario()
    .AddObterConsolidadoPeriodo();

// Infrastructure: EF Core, Redis, IOutboxStore
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxProcessor>();

// Azure-specific: Service Bus (IMessageBus) + Key Vault
builder.Services.AddAzureInfrastructure(builder.Configuration);

builder.Build().Run();

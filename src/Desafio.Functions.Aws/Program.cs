using Desafio.Features.Common.Auth;
using Desafio.Features.Consolidado.ObterDiario;
using Desafio.Features.Consolidado.ObterPeriodo;
using Desafio.Features.Lancamentos.Criar;
using Desafio.Features.Lancamentos.Listar;
using Desafio.Features.Lancamentos.ObterPorId;
using Desafio.Infrastructure;
using Desafio.Infrastructure.Aws;

// When running locally: dotnet run
// When deployed to AWS Lambda: this is the entry point bootstrapped by LambdaEntryPoint

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Vertical slice handlers — identical to Desafio.Server
builder.Services
    .AddCriarLancamento()
    .AddListarLancamentos()
    .AddObterLancamentoPorId()
    .AddObterConsolidadoDiario()
    .AddObterConsolidadoPeriodo();

// Infrastructure: EF Core, Redis, IOutboxStore
builder.Services.AddInfrastructure(builder.Configuration);

// AWS-specific: SQS (IMessageBus) + Secrets Manager
builder.Services.AddAwsInfrastructure();

// Register Lambda hosting (no-op when running locally)
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

// AWS auth mocks — mirror pattern of Desafio.Server but with Cognito/ApiGateway
app.UseMiddleware<MockApiGatewayMiddleware>();
app.UseMiddleware<MockCognitoMiddleware>();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// All business endpoints — same Map*() extensions from each slice
var api = app.MapGroup("/api");
api.MapCriarLancamento();
api.MapListarLancamentos();
api.MapObterLancamentoPorId();
api.MapObterConsolidadoDiario();
api.MapObterConsolidadoPeriodo();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .WithTags("Health");

app.Run();

// Expose Program for integration tests and Lambda bootstrap
public partial class Program;

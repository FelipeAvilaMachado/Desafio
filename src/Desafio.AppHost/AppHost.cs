// =============================================================================
// Desafio — Cash Flow Challenge
// .NET Aspire Orchestrator (local dev + Azure provisioning via `azd provision`)
//
// Azure path:  lancamentos API  →  Azure Service Bus  →  consolidado worker
// AWS path:    deploy Desafio.Functions.Aws to Lambda + API Gateway (SAM/CDK)
//              Desafio.Consolidado.Worker → ECS Fargate task or Lambda schedule
// =============================================================================

var builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure ────────────────────────────────────────────────────────────
var sqlServer = builder.AddSqlServer("sqlserver");

var lancamentosDb = sqlServer.AddDatabase("lancamentosdb");

var cache = builder.AddRedis("cache");

var messaging = builder.AddRabbitMQ("messaging");

// ── Services ──────────────────────────────────────────────────────────────────

// Lancamentos Minimal API (Desafio.Server)
var lancamentos = builder.AddProject<Projects.Desafio_Server>("lancamentos")
    .WithReference(lancamentosDb)
    .WithReference(cache)
    .WithReference(messaging)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile(container => container.WithContainerRuntimeArgs("--cpus", "2", "--memory", "2g"))
    .WaitFor(lancamentosDb)
    .WaitFor(cache)
    .WaitFor(messaging);

// Consolidado background worker
var consolidado = builder.AddProject<Projects.Desafio_Consolidado_Worker>("consolidado")
    .WithReference(lancamentosDb)
    .WithReference(cache)
    .WithReference(messaging)
    .WaitFor(lancamentosDb)
    .WaitFor(cache)
    .WaitFor(messaging);

// Vite frontend (wired to the Lancamentos API)
var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(lancamentos)
    .WaitFor(lancamentos);

lancamentos.PublishWithContainerFiles(webfrontend, "wwwroot");

// ── NOTE: Azure Functions host (Desafio.Functions.Azure) ─────────────────────
// Run locally: cd Desafio.Functions.Azure && func start
// The Functions project is not added to Aspire orchestration because it requires
// the Azure Functions Core Tools runtime. In production, deploy via:
//   az functionapp deployment (Azure portal) or GitHub Actions.

// ── NOTE: AWS Lambda host (Desafio.Functions.Aws) ────────────────────────────
// Run locally: cd Desafio.Functions.Aws && dotnet run
// Deploy to AWS: cd Desafio.Functions.Aws && dotnet lambda deploy-function
//            OR: cd infra/aws && sam deploy --guided

builder.Build().Run();


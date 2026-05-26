// =============================================================================
// Desafio — Cash Flow Challenge
// .NET Aspire Orchestrator (local dev + Azure provisioning via `azd provision`)
//
// Azure path:  lancamentos API  →  Azure Service Bus  →  consolidado worker
// AWS path:    deploy Desafio.Functions.Aws to Lambda + API Gateway (SAM/CDK)
//              Desafio.Consolidado.Worker → ECS Fargate task or Lambda schedule
// =============================================================================

var builder = DistributedApplication.CreateBuilder(args);

// Select cloud simulator with DESAFIO_SIMULATOR=aws|azure (default: aws).
var simulator = (builder.Configuration["DESAFIO_SIMULATOR"] ?? "aws").Trim().ToLowerInvariant();
var useAzureSimulator = simulator == "azure";

// ── Infrastructure ────────────────────────────────────────────────────────────
var sqlServer = builder.AddSqlServer("sqlserver");

var lancamentosDb = sqlServer.AddDatabase("lancamentosdb");

// Read-replica: same server in local dev; swap connection string in production
var sqlReadOnly = sqlServer.AddDatabase("lancamentosdb-readonly");

var cache = builder.AddRedis("cache");

var messaging = builder.AddRabbitMQ("messaging");

var cloudSimulator = useAzureSimulator
    ? builder.AddContainer("floci-az", "floci/floci-az", "latest")
        .WithEndpoint(targetPort: 4577, port: 4577, scheme: "http", name: "http")
        .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock")
    : builder.AddContainer("floci", "floci/floci", "latest")
        .WithEndpoint(targetPort: 4566, port: 4566, scheme: "http", name: "http")
        .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock");

// ── Services ──────────────────────────────────────────────────────────────────

// Lancamentos Minimal API (Desafio.Server)
var lancamentos = builder.AddProject<Projects.Desafio_Server>("lancamentos")
    .WithReference(lancamentosDb)
    .WithReference(sqlReadOnly)
    .WithReference(cache)
    .WithReference(messaging)
    .WithEnvironment("DESAFIO_SIMULATOR", simulator)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WaitFor(cloudSimulator);

// Consolidado background worker
var consolidado = builder.AddProject<Projects.Desafio_Consolidado_Worker>("consolidado")
    .WithReference(lancamentosDb)
    .WithReference(sqlReadOnly)
    .WithReference(cache)
    .WithReference(messaging)
    .WithEnvironment("DESAFIO_SIMULATOR", simulator)
    .WaitFor(lancamentosDb)
    .WaitFor(sqlReadOnly)
    .WaitFor(cache)
    .WaitFor(messaging)
    .WaitFor(cloudSimulator);

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


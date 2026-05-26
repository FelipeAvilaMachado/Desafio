using Desafio.Features.Common.Auth;
using Desafio.Features.Consolidado.ObterDiario;
using Desafio.Features.Consolidado.ObterPeriodo;
using Desafio.Features.Lancamentos.Criar;
using Desafio.Features.Lancamentos.Listar;
using Desafio.Features.Lancamentos.ObterPorId;
using Desafio.Infrastructure;
using Desafio.Infrastructure.Aws;

namespace Desafio.Functions.Aws;

/// <summary>
/// ASP.NET Core startup used by <see cref="LambdaEntryPoint"/>.
/// Mirrors Program.cs — kept separate so the Lambda bootstrap doesn't call WebApplication.CreateBuilder.
/// </summary>
public sealed class LambdaStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddProblemDetails();

        services
            .AddCriarLancamento()
            .AddListarLancamentos()
            .AddObterLancamentoPorId()
            .AddObterConsolidadoDiario()
            .AddObterConsolidadoPeriodo();

        services.AddInfrastructure(configuration);
        services.AddAwsInfrastructure();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseMiddleware<MockApiGatewayMiddleware>();
        app.UseMiddleware<MockCognitoMiddleware>();
        app.UseExceptionHandler("/error");
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
            var api = endpoints.MapGroup("/api");
            api.MapCriarLancamento();
            api.MapListarLancamentos();
            api.MapObterLancamentoPorId();
            api.MapObterConsolidadoDiario();
            api.MapObterConsolidadoPeriodo();
        });
    }
}

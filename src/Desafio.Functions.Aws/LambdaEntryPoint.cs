using Amazon.Lambda.AspNetCoreServer;

namespace Desafio.Functions.Aws;

/// <summary>
/// Lambda entry point for HTTP API (payload format v2).
/// API Gateway routes all requests through the ASP.NET Core pipeline defined in Program.cs.
/// Deploy: sam deploy --guided  OR  dotnet lambda deploy-function
/// </summary>
public sealed class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
        => builder.UseStartup<LambdaStartup>();
}

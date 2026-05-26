using Amazon.SecretsManager;
using Amazon.SQS;
using Desafio.Features.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Desafio.Infrastructure.Aws;

public static class AwsInfrastructureServiceExtensions
{
    /// <summary>
    /// Registers AWS-specific infrastructure: SQS (IMessageBus) and Secrets Manager.
    /// Call after <c>AddInfrastructure()</c> from <c>Desafio.Infrastructure</c>.
    /// </summary>
    public static IServiceCollection AddAwsInfrastructure(this IServiceCollection services)
    {
        services.AddAWSService<IAmazonSQS>();
        services.AddAWSService<IAmazonSecretsManager>();

        services.AddSingleton<IMessageBus, SqsMessageBus>();
        services.AddSingleton<SecretsManagerProvider>();

        return services;
    }
}

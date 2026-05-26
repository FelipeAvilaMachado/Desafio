using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Desafio.Features.Common.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Desafio.Infrastructure.Azure;

public static class AzureInfrastructureServiceExtensions
{
    /// <summary>
    /// Registers Azure-specific infrastructure: Service Bus (IMessageBus) and Key Vault.
    /// Call after <c>AddInfrastructure()</c> from <c>Desafio.Infrastructure</c>.
    /// </summary>
    public static IServiceCollection AddAzureInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Azure Service Bus
        var sbConnectionString = configuration.GetConnectionString("messaging")
            ?? configuration["Azure:ServiceBus:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(sbConnectionString))
        {
            try
            {
                _ = ServiceBusConnectionStringProperties.Parse(sbConnectionString);
                services.AddSingleton(new ServiceBusClient(sbConnectionString));
                services.AddSingleton<IMessageBus, AzureServiceBusMessageBus>();
            }
            catch (FormatException)
            {
                services.AddSingleton<IMessageBus, NoOpMessageBus>();
            }
        }
        else
        {
            services.AddSingleton<IMessageBus, NoOpMessageBus>();
        }

        // Azure Key Vault
        var kvUri = configuration["Azure:KeyVault:Uri"];
        if (!string.IsNullOrWhiteSpace(kvUri))
        {
            services.AddSingleton(new SecretClient(new Uri(kvUri), new DefaultAzureCredential()));
            services.AddSingleton<AzureKeyVaultSecretProvider>();
        }

        return services;
    }
}

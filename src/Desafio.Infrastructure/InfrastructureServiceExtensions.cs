using Desafio.Features.Common.Messaging;
using Desafio.Features.Common.Outbox;
using Desafio.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Desafio.Infrastructure;

public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers EF Core DbContexts, Redis, and outbox store.
    /// Call <c>AddAzureInfrastructure</c> or <c>AddAwsInfrastructure</c> afterwards
    /// to plug in the cloud-specific <see cref="IMessageBus"/> implementation.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Write DbContext (SQL Server)
        services.AddDbContext<LancamentosDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("lancamentosdb")));

        services.AddScoped<ILancamentosDbContext>(sp =>
            sp.GetRequiredService<LancamentosDbContext>());

        // Consolidado read-model DbContext (same database as write model in local/dev)
        services.AddDbContext<ConsolidadoReadDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("lancamentosdb"))
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IConsolidadoReadDbContext>(sp =>
            sp.GetRequiredService<ConsolidadoReadDbContext>());

        // Outbox store
        services.AddScoped<IOutboxStore, EfOutboxStore>();

        // Redis
        var redisCs = configuration.GetConnectionString("cache");
        if (!string.IsNullOrWhiteSpace(redisCs))
        {
            // Keep retrying in local/dev orchestration instead of failing app startup.
            var redisOptions = ConfigurationOptions.Parse(redisCs, true);
            redisOptions.AbortOnConnectFail = false;

            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisOptions));
        }

        return services;
    }
}

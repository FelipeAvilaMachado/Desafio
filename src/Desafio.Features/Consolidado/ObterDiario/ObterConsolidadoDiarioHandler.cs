using System.Text.Json;
using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Desafio.Features.Consolidado.ObterDiario;

public sealed class ObterConsolidadoDiarioHandler(
    IConsolidadoReadDbContext db,
    IConnectionMultiplexer redis,
    ILogger<ObterConsolidadoDiarioHandler> logger)
    : IHandler<ObterConsolidadoDiarioQuery, ConsolidadoDiarioDto?>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<ConsolidadoDiarioDto?> HandleAsync(ObterConsolidadoDiarioQuery request, CancellationToken ct = default)
    {
        var cacheKey = $"consolidado:diario:{request.Data:yyyy-MM-dd}";
        var cache = redis.GetDatabase();

        var cached = await cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            logger.LogInformation("Cache HIT for consolidado diario {Data}", request.Data);
            return JsonSerializer.Deserialize<ConsolidadoDiarioDto>((string)cached!);
        }

        logger.LogWarning("Cache MISS for consolidado diario {Data} — querying read replica", request.Data);

        var consolidado = await db.ConsolidadosDiarios
            .FirstOrDefaultAsync(c => c.Data == request.Data, ct);

        if (consolidado is null)
        {
            logger.LogInformation("ConsolidadoDiario não encontrado para {Data}", request.Data);
            return null;
        }

        var dto = ConsolidadoDiarioDto.FromEntity(consolidado);
        await cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(dto), CacheTtl);

        logger.LogInformation(
            "ConsolidadoDiario {Data}: Saldo={Saldo}, Debitos={Debitos}, Creditos={Creditos}",
            request.Data, dto.Saldo, dto.TotalDebitos, dto.TotalCreditos);

        return dto;
    }
}

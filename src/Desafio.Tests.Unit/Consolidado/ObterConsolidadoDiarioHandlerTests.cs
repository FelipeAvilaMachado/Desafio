using System.Text.Json;
using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Entities;
using Desafio.Features.Common.Outbox;
using Desafio.Features.Common.Persistence;
using Desafio.Features.Consolidado.ObterDiario;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StackExchange.Redis;

namespace Desafio.Tests.Unit.Consolidado;

/// <summary>
/// In-memory DbContext that implements <see cref="IConsolidadoReadDbContext"/>.
/// Used in tests in place of the sealed production <c>ConsolidadoReadDbContext</c>.
/// </summary>
sealed class TestConsolidadoDbContext(DbContextOptions<TestConsolidadoDbContext> options)
    : DbContext(options), IConsolidadoReadDbContext
{
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<ConsolidadoDiario> ConsolidadosDiarios => Set<ConsolidadoDiario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lancamento>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Tipo).HasConversion<string>();
        });

        modelBuilder.Entity<ConsolidadoDiario>(e =>
        {
            e.HasKey(c => c.Id);
            e.Ignore(c => c.Saldo);
        });
    }
}

public sealed class ObterConsolidadoDiarioHandlerTests : IDisposable
{
    private readonly TestConsolidadoDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _redisDb;
    private readonly ILogger<ObterConsolidadoDiarioHandler> _logger;
    private readonly ObterConsolidadoDiarioHandler _handler;

    public ObterConsolidadoDiarioHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestConsolidadoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new TestConsolidadoDbContext(options);
        _redis = Substitute.For<IConnectionMultiplexer>();
        _redisDb = Substitute.For<IDatabase>();
        _redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_redisDb);
        _logger = Substitute.For<ILogger<ObterConsolidadoDiarioHandler>>();
        _handler = new ObterConsolidadoDiarioHandler(_db, _redis, _logger);
    }

    [Fact]
    public async Task HandleAsync_CacheMiss_DbHit_ReturnsDto()
    {
        var data = DateOnly.FromDateTime(DateTime.Today);
        _redisDb.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
                .Returns(RedisValue.Null);

        var consolidado = ConsolidadoDiario.Calcular(data, new[]
        {
            Lancamento.Criar(TipoLancamento.Credito, 1000m, "Venda", data),
            Lancamento.Criar(TipoLancamento.Debito, 300m, "Custo", data),
        });

        _db.ConsolidadosDiarios.Add(consolidado);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(new ObterConsolidadoDiarioQuery(data));

        result.Should().NotBeNull();
        result!.TotalCreditos.Should().Be(1000m);
        result.TotalDebitos.Should().Be(300m);
        result.Saldo.Should().Be(700m);
    }

    [Fact]
    public async Task HandleAsync_CacheHit_ReturnsDtoFromCache()
    {
        var data = DateOnly.FromDateTime(DateTime.Today);
        var dto = new ConsolidadoDiarioDto(data, 300m, 1000m, 700m, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(dto);

        _redisDb.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
                .Returns(new RedisValue(json));

        var result = await _handler.HandleAsync(new ObterConsolidadoDiarioQuery(data));

        result.Should().NotBeNull();
        result!.TotalCreditos.Should().Be(1000m);
        result.Saldo.Should().Be(700m);
    }

    [Fact]
    public async Task HandleAsync_CacheMiss_DbMiss_ReturnsNull()
    {
        _redisDb.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
                .Returns(RedisValue.Null);

        var result = await _handler.HandleAsync(
            new ObterConsolidadoDiarioQuery(DateOnly.FromDateTime(DateTime.Today.AddDays(-99))));

        result.Should().BeNull();
    }

    public void Dispose() => _db.Dispose();
}

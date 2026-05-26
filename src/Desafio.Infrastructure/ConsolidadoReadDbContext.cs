using Desafio.Features.Common.Entities;
using Desafio.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Desafio.Infrastructure;

/// <summary>
/// DbContext used by the consolidado read model.
/// It can be updated by background/manual consolidation processes.
/// </summary>
public sealed class ConsolidadoReadDbContext(DbContextOptions<ConsolidadoReadDbContext> options)
    : DbContext(options), IConsolidadoReadDbContext
{
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<ConsolidadoDiario> ConsolidadosDiarios => Set<ConsolidadoDiario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lancamento>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Tipo).HasConversion<string>().HasMaxLength(10);
            e.Property(l => l.Valor).HasColumnType("decimal(18,2)");
            e.Property(l => l.Descricao).HasMaxLength(500);
            e.HasIndex(l => l.Data);
        });

        modelBuilder.Entity<ConsolidadoDiario>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.TotalDebitos).HasColumnType("decimal(18,2)");
            e.Property(c => c.TotalCreditos).HasColumnType("decimal(18,2)");
            e.Ignore(c => c.Saldo);
            e.HasIndex(c => c.Data).IsUnique();
        });
    }

    public Task UpsertConsolidadoDiarioAsync(ConsolidadoDiario consolidado, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var newId = Guid.NewGuid();

        return Database.ExecuteSqlInterpolatedAsync(
            $"""
            MERGE [dbo].[ConsolidadosDiarios] WITH (HOLDLOCK) AS target
            USING (VALUES ({consolidado.Data}, {consolidado.TotalDebitos}, {consolidado.TotalCreditos}, {now}))
            AS source ([Data], [TotalDebitos], [TotalCreditos], [AtualizadoEm])
            ON target.[Data] = source.[Data]
            WHEN MATCHED THEN
                UPDATE SET
                    [TotalDebitos] = source.[TotalDebitos],
                    [TotalCreditos] = source.[TotalCreditos],
                    [AtualizadoEm] = source.[AtualizadoEm]
            WHEN NOT MATCHED THEN
                INSERT ([Id], [Data], [TotalDebitos], [TotalCreditos], [AtualizadoEm])
                VALUES ({newId}, source.[Data], source.[TotalDebitos], source.[TotalCreditos], source.[AtualizadoEm]);
            """,
            ct);
    }
}

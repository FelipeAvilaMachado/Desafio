using Desafio.Features.Common.Entities;
using Desafio.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Desafio.Infrastructure;

/// <summary>
/// Read-only DbContext pointing at the SQL read replica.
/// Never used for writes — SaveChanges throws.
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

    public override int SaveChanges() => throw new InvalidOperationException("This context is read-only.");
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        throw new InvalidOperationException("This context is read-only.");
}

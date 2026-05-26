using Desafio.Features.Common.Entities;
using Desafio.Features.Common.Outbox;
using Desafio.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Desafio.Infrastructure;

public sealed class LancamentosDbContext(DbContextOptions<LancamentosDbContext> options)
    : DbContext(options), ILancamentosDbContext
{
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

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

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.EventType).HasMaxLength(500);
            e.HasIndex(o => o.ProcessedAt);
        });
    }
}

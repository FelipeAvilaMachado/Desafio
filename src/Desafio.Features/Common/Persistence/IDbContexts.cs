using Desafio.Features.Common.Entities;
using Desafio.Features.Common.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Desafio.Features.Common.Persistence;

/// <summary>
/// Write-side DbContext abstraction. Implemented in <c>Desafio.Infrastructure</c>.
/// </summary>
public interface ILancamentosDbContext
{
    DbSet<Lancamento> Lancamentos { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Read-only DbContext abstraction (maps to the SQL read replica).
/// </summary>
public interface IConsolidadoReadDbContext
{
    DbSet<Lancamento> Lancamentos { get; }
    DbSet<ConsolidadoDiario> ConsolidadosDiarios { get; }
}

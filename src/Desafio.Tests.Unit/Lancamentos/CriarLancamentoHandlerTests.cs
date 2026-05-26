using Desafio.Features.Common.Entities;
using Desafio.Features.Common.Outbox;
using Desafio.Features.Common.Persistence;
using Desafio.Features.Lancamentos.Criar;
using Desafio.Infrastructure;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Desafio.Tests.Unit.Lancamentos;

public sealed class CriarLancamentoHandlerTests : IDisposable
{
    private readonly LancamentosDbContext _db;
    private readonly IOutboxStore _outbox;
    private readonly ILogger<CriarLancamentoHandler> _logger;
    private readonly CriarLancamentoHandler _handler;

    public CriarLancamentoHandlerTests()
    {
        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new LancamentosDbContext(options);
        _outbox = Substitute.For<IOutboxStore>();
        _logger = Substitute.For<ILogger<CriarLancamentoHandler>>();
        _handler = new CriarLancamentoHandler(
            _db, _outbox, new CriarLancamentoValidator(), _logger);
    }

    [Fact]
    public async Task HandleAsync_ValidCredito_ReturnsDtoAndPersists()
    {
        var command = new CriarLancamentoCommand("Credito", 500m, "Venda", DateOnly.FromDateTime(DateTime.Today));

        var dto = await _handler.HandleAsync(command);

        dto.Should().NotBeNull();
        dto.Tipo.Should().Be("Credito");
        dto.Valor.Should().Be(500m);
        _db.Lancamentos.Should().HaveCount(1);
        await _outbox.Received(1).SaveAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidDebito_ReturnsDtoAndPersists()
    {
        var command = new CriarLancamentoCommand("Debito", 100m, "Aluguel", DateOnly.FromDateTime(DateTime.Today));

        var dto = await _handler.HandleAsync(command);

        dto.Tipo.Should().Be("Debito");
        dto.Valor.Should().Be(100m);
        _db.Lancamentos.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_ValorZero_ThrowsValidationException()
    {
        var command = new CriarLancamentoCommand("Credito", 0m, "Zero value", DateOnly.FromDateTime(DateTime.Today));

        var act = () => _handler.HandleAsync(command);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Valor must be greater than zero*");
    }

    [Fact]
    public async Task HandleAsync_TipoInvalido_ThrowsValidationException()
    {
        var command = new CriarLancamentoCommand("Pix", 50m, "Invalid type", DateOnly.FromDateTime(DateTime.Today));

        var act = () => _handler.HandleAsync(command);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Tipo must be 'Debito' or 'Credito'*");
    }

    [Fact]
    public async Task HandleAsync_DescricaoVazia_ThrowsValidationException()
    {
        var command = new CriarLancamentoCommand("Credito", 100m, "", DateOnly.FromDateTime(DateTime.Today));

        var act = () => _handler.HandleAsync(command);

        await act.Should().ThrowAsync<ValidationException>();
    }

    public void Dispose() => _db.Dispose();
}

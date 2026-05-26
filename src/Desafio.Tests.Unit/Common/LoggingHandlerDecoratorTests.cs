using Desafio.Features.Common.Dtos;
using Desafio.Features.Common.Entities;
using Desafio.Features.Common.Handlers;
using Desafio.Features.Common.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Desafio.Tests.Unit.Common;

public sealed class LoggingHandlerDecoratorTests
{
    private readonly IHandler<TestRequest, TestResponse> _inner;
    private readonly ILogger<LoggingHandlerDecorator<TestRequest, TestResponse>> _logger;
    private readonly LoggingHandlerDecorator<TestRequest, TestResponse> _decorator;

    public LoggingHandlerDecoratorTests()
    {
        _inner = Substitute.For<IHandler<TestRequest, TestResponse>>();
        _logger = Substitute.For<ILogger<LoggingHandlerDecorator<TestRequest, TestResponse>>>();
        _decorator = new LoggingHandlerDecorator<TestRequest, TestResponse>(_inner, _logger);
    }

    [Fact]
    public async Task HandleAsync_Success_ReturnsInnerResult()
    {
        var request = new TestRequest("hello");
        var expected = new TestResponse("world");
        _inner.HandleAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _decorator.HandleAsync(request);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task HandleAsync_Exception_RethrowsAfterLogging()
    {
        var request = new TestRequest("fail");
        _inner.HandleAsync(request, Arg.Any<CancellationToken>())
              .ThrowsAsync(new InvalidOperationException("boom"));

        var act = () => _decorator.HandleAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public async Task HandleAsync_CallsInnerExactlyOnce()
    {
        var request = new TestRequest("once");
        _inner.HandleAsync(request, Arg.Any<CancellationToken>())
              .Returns(new TestResponse("ok"));

        await _decorator.HandleAsync(request);

        await _inner.Received(1).HandleAsync(request, Arg.Any<CancellationToken>());
    }

    // ── Test stubs ───────────────────────────────────────────────────────────
    public sealed record TestRequest(string Value);
    public sealed record TestResponse(string Result);
}

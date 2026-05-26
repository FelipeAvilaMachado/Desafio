namespace Desafio.Features.Common.Outbox;

/// <summary>
/// Persistent record written atomically with the business transaction.
/// The <see cref="OutboxProcessor"/> polls this table and publishes to the message bus.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Full CLR type name of the event payload.</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>JSON-serialised event payload.</summary>
    public string Payload { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}

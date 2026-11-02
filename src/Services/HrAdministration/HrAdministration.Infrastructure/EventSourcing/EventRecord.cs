namespace HrAdministration.Infrastructure.EventSourcing;

/// <summary>A row of the <c>events</c> table. Its properties are init-only: a stored event never changes (C17).</summary>
public sealed class EventRecord
{
    public required string StreamId { get; init; }

    /// <summary>Position in the stream, from 1.</summary>
    public required int Version { get; init; }

    public required string Type { get; init; }

    public required string Data { get; init; }

    public required string Metadata { get; init; }

    public required DateTimeOffset RecordedAt { get; init; }
}

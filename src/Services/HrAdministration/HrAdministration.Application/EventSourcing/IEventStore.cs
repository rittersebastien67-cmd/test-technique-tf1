using SharedKernel;

namespace HrAdministration.Application.EventSourcing;

/// <summary>Port of the store of the event-sourced aggregates (AD48). It can only read and append: the store is the audit trail (C17).</summary>
public interface IEventStore
{
    /// <summary>The events of a stream in the order they were appended. A stream that does not exist is empty.</summary>
    Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(string streamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends to several streams in a single transaction: all of them are written, or none. If a stream has moved since it was read,
    /// the result is <see cref="EventStoreErrors.ConcurrencyConflict"/> and nothing is written.
    /// </summary>
    Task<Result> AppendAsync(IReadOnlyList<StreamAppend> appends, EventMetadata metadata, CancellationToken cancellationToken = default);
}

using SharedKernel;

namespace HrAdministration.Application.EventSourcing;

/// <summary>New events of a stream. <paramref name="ExpectedVersion"/> is the number of events read before deciding, as in <c>AggregateRoot.Version</c>.</summary>
public sealed record StreamAppend(string StreamId, int ExpectedVersion, IReadOnlyList<IDomainEvent> Events);

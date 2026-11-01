namespace SharedKernel;

/// <summary>
/// Consistency boundary whose state only changes through domain events:
/// <see cref="Raise"/> applies a new event, <see cref="LoadFromHistory"/> replays persisted ones.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>Number of persisted events; the expected version when appending the uncommitted ones.</summary>
    public int Version { get; private set; }

    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents;

    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        if (_uncommittedEvents.Count > 0)
        {
            throw new InvalidOperationException("Cannot replay history over uncommitted events.");
        }

        foreach (var domainEvent in history)
        {
            Apply(domainEvent);
            Version++;
        }
    }

    public void MarkEventsAsCommitted()
    {
        Version += _uncommittedEvents.Count;
        _uncommittedEvents.Clear();
    }

    protected void Raise(IDomainEvent domainEvent)
    {
        Apply(domainEvent);
        _uncommittedEvents.Add(domainEvent);
    }

    /// <summary>Mutates state from an event. Must not validate nor fail: rules are checked before raising.</summary>
    protected abstract void Apply(IDomainEvent domainEvent);
}

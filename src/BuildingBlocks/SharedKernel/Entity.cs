namespace SharedKernel;

/// <summary>
/// An object defined by its identity. Two entities are equal when they share type and id;
/// an entity whose id is not assigned yet is only equal to itself.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity()
    {
        Id = default!;
    }

    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>Settable by subclasses so event-sourced aggregates can assign it while replaying.</summary>
    public TId Id { get; protected set; }

    private bool IsTransient => EqualityComparer<TId>.Default.Equals(Id, default!);

    public bool Equals(Entity<TId>? other)
    {
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return !IsTransient && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() =>
        IsTransient ? base.GetHashCode() : HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}

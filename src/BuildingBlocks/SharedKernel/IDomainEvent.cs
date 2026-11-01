namespace SharedKernel;

/// <summary>
/// A fact raised by an aggregate. Implementations stay serialization-agnostic:
/// no attributes, no infrastructure types.
/// </summary>
public interface IDomainEvent;

using HrAdministration.Domain.Leaves;

namespace HrAdministration.Application.EventSourcing;

/// <summary>Who wrote the events of a transaction, for the audit (C17).</summary>
/// <param name="Actor">Who acted, according to the scope of the token.</param>
/// <param name="Origin">Who prepared the request. An employee can submit a draft of the agent, so it can differ from <paramref name="Actor"/>.</param>
/// <param name="CorrelationId">Links the events of one operation. A GUID, so that no free text can end up in it.</param>
public sealed record EventMetadata(Actor Actor, Origin Origin, Guid CorrelationId);

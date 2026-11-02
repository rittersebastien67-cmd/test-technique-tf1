using HrAdministration.Infrastructure.EventSourcing;
using SharedKernel;

namespace HrAdministration.Infrastructure.Projections;

/// <summary>
/// Read model kept up to date from the events. It only changes the entities tracked by <c>context</c> and never saves them: the store saves them
/// with the events, so a conflict leaves the read model untouched. Each row depends on one stream only, so streams can be replayed in any order.
/// </summary>
public interface IInlineProjection
{
    /// <summary>Ignores the events it does not read.</summary>
    Task ApplyAsync(HrAdministrationDbContext context, string streamId, IDomainEvent domainEvent, CancellationToken cancellationToken);

    /// <summary>Empties the read model before a replay.</summary>
    Task ClearAsync(HrAdministrationDbContext context, CancellationToken cancellationToken);
}

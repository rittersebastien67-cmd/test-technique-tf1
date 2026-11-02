using HrAdministration.Infrastructure.EventSourcing;
using Microsoft.EntityFrameworkCore;

namespace HrAdministration.Infrastructure.Projections;

/// <summary>
/// Rebuilds the read models by replaying the events, through the code of the inline projections. All or nothing, in one transaction.
/// An append that commits during the replay would be missed, so it runs before the host accepts requests.
/// </summary>
public sealed class ProjectionRebuilder
{
    private readonly HrAdministrationDbContext _context;
    private readonly IEnumerable<IInlineProjection> _projections;

    public ProjectionRebuilder(HrAdministrationDbContext context, IEnumerable<IInlineProjection> projections)
    {
        _context = context;
        _projections = projections;
    }

    public async Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        foreach (var projection in _projections)
        {
            await projection.ClearAsync(_context, cancellationToken);
        }

        var records = await _context.Events
            .AsNoTracking()
            .OrderBy(record => record.StreamId)
            .ThenBy(record => record.Version)
            .ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            var domainEvent = EventSerializer.Deserialize(record.Type, record.Data);

            foreach (var projection in _projections)
            {
                await projection.ApplyAsync(_context, record.StreamId, domainEvent, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

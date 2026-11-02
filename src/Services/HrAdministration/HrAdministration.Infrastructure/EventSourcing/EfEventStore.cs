using HrAdministration.Application.EventSourcing;
using HrAdministration.Infrastructure.Projections;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace HrAdministration.Infrastructure.EventSourcing;

/// <summary>
/// Event store on EF Core (AD49). A single <c>SaveChanges</c> makes one transaction of an append, whatever the number of streams,
/// with the changes of the inline projections.
/// </summary>
public sealed class EfEventStore : IEventStore
{
    private readonly HrAdministrationDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly IUniqueViolationDetector _uniqueViolations;
    private readonly IEnumerable<IInlineProjection> _projections;

    public EfEventStore(
        HrAdministrationDbContext context,
        TimeProvider timeProvider,
        IUniqueViolationDetector uniqueViolations,
        IEnumerable<IInlineProjection> projections)
    {
        _context = context;
        _timeProvider = timeProvider;
        _uniqueViolations = uniqueViolations;
        _projections = projections;
    }

    public async Task<IReadOnlyList<IDomainEvent>> ReadStreamAsync(string streamId, CancellationToken cancellationToken = default)
    {
        var records = await _context.Events
            .AsNoTracking()
            .Where(record => record.StreamId == streamId)
            .OrderBy(record => record.Version)
            .ToListAsync(cancellationToken);

        return [.. records.Select(record => EventSerializer.Deserialize(record.Type, record.Data))];
    }

    public async Task<Result> AppendAsync(IReadOnlyList<StreamAppend> appends, EventMetadata metadata, CancellationToken cancellationToken = default)
    {
        var recordedAt = _timeProvider.GetUtcNow();
        var serializedMetadata = EventSerializer.SerializeMetadata(metadata);

        try
        {
            _context.Events.AddRange(appends.SelectMany(append => append.Events.Select((domainEvent, index) => new EventRecord
            {
                StreamId = append.StreamId,
                Version = append.ExpectedVersion + index + 1,
                Type = EventSerializer.TypeName(domainEvent),
                Data = EventSerializer.SerializeData(domainEvent),
                Metadata = serializedMetadata,
                RecordedAt = recordedAt,
            })));

            foreach (var append in appends)
            {
                foreach (var domainEvent in append.Events)
                {
                    foreach (var projection in _projections)
                    {
                        await projection.ApplyAsync(_context, append.StreamId, domainEvent, cancellationToken);
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException exception) when (_uniqueViolations.IsUniqueViolation(exception))
        {
            return EventStoreErrors.ConcurrencyConflict;
        }
        finally
        {
            // A key left in the tracker would turn the next conflict in this scope into an EF Core exception.
            _context.ChangeTracker.Clear();
        }
    }
}

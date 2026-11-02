using HrAdministration.Application.EventSourcing;
using HrAdministration.Domain.Balances;
using HrAdministration.Domain.Leaves;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using static HrAdministration.Infrastructure.Tests.EventSourcing.SampleEvents;

namespace HrAdministration.Infrastructure.Tests.EventSourcing;

public sealed class EfEventStoreTests : IDisposable
{
    private const string Stream = "stream-a";
    private const string OtherStream = "stream-b";

    private static readonly EventMetadata Metadata = new(Actor.Employee, Origin.Manual, Guid.NewGuid());

    private static readonly IDomainEvent First = new DaysCredited(Guid.NewGuid(), 1m);
    private static readonly IDomainEvent Second = new DaysCredited(Guid.NewGuid(), 2m);
    private static readonly IDomainEvent Third = new DaysCredited(Guid.NewGuid(), 3m);

    private readonly SqliteEventStoreDatabase _database = new();

    public void Dispose() => _database.Dispose();

    // Reading and appending.

    [Fact]
    public async Task A_stream_that_does_not_exist_is_empty()
    {
        Assert.Empty(await _database.NewStore().ReadStreamAsync(Stream));
    }

    [Fact]
    public async Task Events_are_read_back_in_the_order_they_were_appended()
    {
        var store = _database.NewStore();

        await AssertSucceeds(store.AppendAsync([new StreamAppend(Stream, 0, [First, Second])], Metadata));
        await AssertSucceeds(store.AppendAsync([new StreamAppend(Stream, 2, [Third])], Metadata));

        Assert.Equal([First, Second, Third], await store.ReadStreamAsync(Stream));
    }

    [Fact]
    public async Task Versions_follow_the_expected_version_from_one()
    {
        var store = _database.NewStore();

        await AssertSucceeds(store.AppendAsync([new StreamAppend(Stream, 0, [First, Second])], Metadata));
        await AssertSucceeds(store.AppendAsync([new StreamAppend(Stream, 2, [Third])], Metadata));

        await using var context = _database.NewContext();
        Assert.Equal([1, 2, 3], await context.Events.OrderBy(record => record.Version).Select(record => record.Version).ToListAsync());
    }

    [Fact]
    public async Task Each_event_is_stored_with_its_type_its_metadata_and_its_date()
    {
        var metadata = new EventMetadata(Actor.Agent, Origin.Agent, Guid.NewGuid());

        await AssertSucceeds(_database.NewStore().AppendAsync([new StreamAppend(Stream, 0, [First])], metadata));

        await using var context = _database.NewContext();
        var record = await context.Events.SingleAsync();
        Assert.Equal(Stream, record.StreamId);
        Assert.Equal(nameof(DaysCredited), record.Type);
        Assert.Contains(metadata.CorrelationId.ToString(), record.Metadata, StringComparison.Ordinal);
        Assert.Contains("\"actor\":\"agent\"", record.Metadata, StringComparison.Ordinal);
        Assert.Equal(SqliteEventStoreDatabase.Now, record.RecordedAt);
    }

    [Fact]
    public async Task An_empty_append_writes_nothing()
    {
        await AssertSucceeds(_database.NewStore().AppendAsync([new StreamAppend(Stream, 0, [])], Metadata));

        Assert.Empty(await _database.NewStore().ReadStreamAsync(Stream));
    }

    // Several streams in one transaction.

    [Fact]
    public async Task Several_streams_are_appended_together()
    {
        var store = _database.NewStore();

        await AssertSucceeds(store.AppendAsync([new StreamAppend(Stream, 0, [First]), new StreamAppend(OtherStream, 0, [Second])], Metadata));

        Assert.Equal([First], await store.ReadStreamAsync(Stream));
        Assert.Equal([Second], await store.ReadStreamAsync(OtherStream));
    }

    [Fact]
    public async Task A_conflict_on_one_stream_writes_nothing_on_the_others()
    {
        var store = _database.NewStore();
        await AssertSucceeds(store.AppendAsync([new StreamAppend(OtherStream, 0, [First])], Metadata));

        var result = await store.AppendAsync([new StreamAppend(Stream, 0, [Second]), new StreamAppend(OtherStream, 0, [Third])], Metadata);

        Assert.Equal(EventStoreErrors.ConcurrencyConflict, result.Error);
        Assert.Empty(await store.ReadStreamAsync(Stream));
        Assert.Equal([First], await store.ReadStreamAsync(OtherStream));
    }

    // Optimistic concurrency.

    [Fact]
    public async Task Appending_at_a_stale_version_is_a_conflict()
    {
        var store = _database.NewStore();
        await AssertSucceeds(store.AppendAsync([new StreamAppend(Stream, 0, [First, Second])], Metadata));

        var result = await store.AppendAsync([new StreamAppend(Stream, 1, [Third])], Metadata);

        Assert.Equal("concurrency.conflict", result.Error?.Code);
        Assert.Equal([First, Second], await store.ReadStreamAsync(Stream));
    }

    [Fact]
    public async Task When_two_requests_read_the_same_version_only_the_first_one_writes()
    {
        var firstRequest = _database.NewStore();
        var secondRequest = _database.NewStore();
        var version = (await firstRequest.ReadStreamAsync(Stream)).Count;
        Assert.Equal(version, (await secondRequest.ReadStreamAsync(Stream)).Count);

        await AssertSucceeds(firstRequest.AppendAsync([new StreamAppend(Stream, version, [First])], Metadata));
        var result = await secondRequest.AppendAsync([new StreamAppend(Stream, version, [Second])], Metadata);

        Assert.Equal(EventStoreErrors.ConcurrencyConflict, result.Error);
        Assert.Equal([First], await firstRequest.ReadStreamAsync(Stream));
    }

    [Fact]
    public async Task A_stale_version_in_the_same_request_is_also_a_conflict()
    {
        var store = _database.NewStore();
        await AssertSucceeds(store.AppendAsync([new StreamAppend(Stream, 0, [First])], Metadata));

        var result = await store.AppendAsync([new StreamAppend(Stream, 0, [Second])], Metadata);

        Assert.Equal(EventStoreErrors.ConcurrencyConflict, result.Error);
    }

    [Fact]
    public async Task The_store_can_append_again_after_a_conflict()
    {
        var store = _database.NewStore();
        await AssertSucceeds(store.AppendAsync([new StreamAppend(Stream, 0, [First])], Metadata));
        Assert.True((await store.AppendAsync([new StreamAppend(Stream, 0, [Second])], Metadata)).IsFailure);

        await AssertSucceeds(store.AppendAsync([new StreamAppend(Stream, 1, [Second])], Metadata));

        Assert.Equal([First, Second], await store.ReadStreamAsync(Stream));
    }

    // Append only.

    [Fact]
    public void The_port_can_only_read_and_append()
    {
        Assert.Equal(
            ["AppendAsync", "ReadStreamAsync"],
            typeof(IEventStore).GetMethods().Select(method => method.Name).Order());
    }

    // With the real aggregates.

    [Fact]
    public async Task A_balance_is_rebuilt_from_its_stream_and_appended_at_its_version()
    {
        var id = new LeaveBalanceId(EmployeeId, BalanceType.PaidLeave, 2026);
        var streamId = StreamIds.Balance(id);
        var store = _database.NewStore();
        var granted = LeaveBalance.Grant(id, 25m, currentYearOnly: false);
        await AssertSucceeds(store.AppendAsync([new StreamAppend(streamId, granted.Version, granted.UncommittedEvents)], Metadata));

        var balance = LeaveBalance.FromHistory(await store.ReadStreamAsync(streamId));
        Assert.True(balance.Deduct(Guid.NewGuid(), 5m, Monday.Date, Friday.Date, Today).IsSuccess);
        await AssertSucceeds(store.AppendAsync([new StreamAppend(streamId, balance.Version, balance.UncommittedEvents)], Metadata));

        var rebuilt = LeaveBalance.FromHistory(await store.ReadStreamAsync(streamId));
        Assert.Equal(id, rebuilt.Id);
        Assert.Equal(20m, rebuilt.Available);
        Assert.Equal(5m, rebuilt.Planned(Today));
        Assert.Equal(2, rebuilt.Version);
    }

    [Fact]
    public async Task A_request_is_rebuilt_from_its_stream_with_its_whole_state()
    {
        var request = Submitted(PaidLeave);
        var streamId = StreamIds.LeaveRequest(request.Id);
        var store = _database.NewStore();

        await AssertSucceeds(store.AppendAsync([new StreamAppend(streamId, request.Version, request.UncommittedEvents)], Metadata));

        var rebuilt = LeaveRequest.FromHistory(await store.ReadStreamAsync(streamId));
        Assert.Equal(request.Id, rebuilt.Id);
        Assert.Equal(EmployeeId, rebuilt.EmployeeId);
        Assert.Equal(Origin.Agent, rebuilt.Origin);
        Assert.Equal(LeaveRequestStatus.Submitted, rebuilt.Status);
        Assert.Equal(new LeaveTypePaidLeave(), rebuilt.Type);
        Assert.Equal(request.Comment, rebuilt.Comment);
        Assert.Equal(5m, rebuilt.Duration!.Total);
        Assert.Equal(request.Flags, rebuilt.Flags);
    }

    [Fact]
    public async Task Events_of_every_type_can_be_stored_and_read()
    {
        var events = OfEveryType();
        var store = _database.NewStore();

        await AssertSucceeds(store.AppendAsync([new StreamAppend(Stream, 0, events)], Metadata));

        var read = await store.ReadStreamAsync(Stream);
        Assert.Equal(events.Select(domainEvent => domainEvent.GetType()), read.Select(domainEvent => domainEvent.GetType()));
    }

    private static async Task AssertSucceeds(Task<Result> append)
    {
        var result = await append;
        Assert.True(result.IsSuccess, result.Error?.Code);
    }
}

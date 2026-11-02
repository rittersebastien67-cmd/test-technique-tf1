using System.Text.Json;
using HrAdministration.Application.EventSourcing;
using HrAdministration.Domain.Balances;
using HrAdministration.Domain.Leaves;
using HrAdministration.Infrastructure.Tests.EventSourcing;
using Microsoft.EntityFrameworkCore;
using static HrAdministration.Infrastructure.Tests.EventSourcing.SampleEvents;

namespace HrAdministration.Infrastructure.Tests.Projections;

public sealed class ProjectionRebuilderTests : IDisposable
{
    private static readonly EventMetadata Metadata = new(Actor.Employee, Origin.Manual, Guid.NewGuid());

    private readonly SqliteEventStoreDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Replaying_the_events_gives_the_same_rows_as_the_inline_projections()
    {
        await AppendEveryKindOfEvent();
        var inline = await Rows();

        await EmptyViews();
        await _database.NewRebuilder().RebuildAsync();

        Assert.Equal(inline, await Rows());
    }

    [Fact]
    public async Task A_replay_replaces_what_the_views_held()
    {
        await AppendEveryKindOfEvent();
        var inline = await Rows();

        await using (var context = _database.NewContext())
        {
            await context.BalancesView.ExecuteUpdateAsync(setters => setters.SetProperty(record => record.Acquired, 999m));
            await context.LeaveRequestsView.Where(record => record.Status == LeaveRequestStatus.Cancelled).ExecuteDeleteAsync();
        }

        await _database.NewRebuilder().RebuildAsync();

        Assert.Equal(inline, await Rows());
    }

    [Fact]
    public async Task Replaying_twice_changes_nothing()
    {
        await AppendEveryKindOfEvent();
        var inline = await Rows();

        await _database.NewRebuilder().RebuildAsync();
        await _database.NewRebuilder().RebuildAsync();

        Assert.Equal(inline, await Rows());
    }

    [Fact]
    public async Task A_replay_does_not_touch_the_events()
    {
        await AppendEveryKindOfEvent();
        var before = await EventRows();

        await _database.NewRebuilder().RebuildAsync();

        Assert.Equal(before, await EventRows());
    }

    [Fact]
    public async Task A_replay_does_not_touch_the_employee_profiles()
    {
        await using (var context = _database.NewContext())
        {
            context.EmployeeProfiles.Add(EmployeeProfileReaderTests.Profile(EmployeeId));
            await context.SaveChangesAsync();
        }

        await _database.NewRebuilder().RebuildAsync();

        await using var check = _database.NewContext();
        Assert.Equal(1, await check.EmployeeProfiles.CountAsync());
    }

    [Fact]
    public async Task A_failed_replay_leaves_the_views_as_they_were()
    {
        await AppendEveryKindOfEvent();
        var inline = await Rows();

        // A balance event whose stream has no entitlement cannot be projected.
        Assert.True((await _database.NewStore().AppendAsync([new StreamAppend("balance-orphan", 0, [new DaysCredited(Guid.NewGuid(), 1m)])], Metadata)).IsSuccess);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _database.NewRebuilder().RebuildAsync());

        Assert.Equal(inline, await Rows());
    }

    [Fact]
    public async Task Replaying_without_any_event_empties_the_views()
    {
        await _database.NewRebuilder().RebuildAsync();

        Assert.Empty(await Rows());
    }

    private async Task AppendEveryKindOfEvent()
    {
        var id = new LeaveBalanceId(EmployeeId, BalanceType.PaidLeave, 2026);
        var balance = LeaveBalance.Grant(id, 25m, currentYearOnly: true);

        var updated = Draft(PaidLeave);
        Assert.True(updated.Update(PaidLeave with { Comment = "Autre", Assumptions = [] }, Today, Calendar).IsSuccess);

        var cancelled = Draft(PaidLeave);
        Assert.True(cancelled.Cancel(Today).IsSuccess);

        var superseded = Submitted(PaidLeave);
        Assert.True(superseded.Supersede().IsSuccess);
        var part = new LeavePart(LeavePeriod.Create(Monday, Friday).Value, superseded.Duration!);
        var carriedOver = superseded.CarryOver(Guid.NewGuid(), part).Value;

        var sickLeave = Submitted(new DraftContent(new LeaveTypeSickLeave(), Monday, Friday, null, []));

        Assert.True(balance.Deduct(superseded.Id, 5m, Monday.Date, Friday.Date, Today).IsSuccess);
        balance.Credit(superseded.Id, 5m);
        balance.DeductCarriedOver(carriedOver.Id, 5m, Monday.Date);

        var result = await _database.NewProjectingStore().AppendAsync(
            [
                new StreamAppend(StreamIds.Balance(id), balance.Version, balance.UncommittedEvents),
                new StreamAppend(StreamIds.LeaveRequest(updated.Id), updated.Version, updated.UncommittedEvents),
                new StreamAppend(StreamIds.LeaveRequest(cancelled.Id), cancelled.Version, cancelled.UncommittedEvents),
                new StreamAppend(StreamIds.LeaveRequest(superseded.Id), superseded.Version, superseded.UncommittedEvents),
                new StreamAppend(StreamIds.LeaveRequest(carriedOver.Id), carriedOver.Version, carriedOver.UncommittedEvents),
                new StreamAppend(StreamIds.LeaveRequest(sickLeave.Id), sickLeave.Version, sickLeave.UncommittedEvents),
            ],
            Metadata);
        Assert.True(result.IsSuccess, result.Error?.Code);
    }

    private async Task EmptyViews()
    {
        await using var context = _database.NewContext();
        await context.LeaveRequestsView.ExecuteDeleteAsync();
        await context.BalancesView.ExecuteDeleteAsync();
    }

    /// <summary>Every column of every row, as text: the comparison does not depend on what the readers expose.</summary>
    private async Task<IReadOnlyList<string>> Rows()
    {
        await using var context = _database.NewContext();
        var requests = await context.LeaveRequestsView.AsNoTracking().ToListAsync();
        var balances = await context.BalancesView.AsNoTracking().ToListAsync();

        return
        [
            .. requests.OrderBy(record => record.LeaveRequestId).Select(record => JsonSerializer.Serialize(record)),
            .. balances.OrderBy(record => record.StreamId).Select(record => JsonSerializer.Serialize(record)),
        ];
    }

    private async Task<IReadOnlyList<string>> EventRows()
    {
        await using var context = _database.NewContext();
        var events = await context.Events.AsNoTracking().ToListAsync();
        return [.. events.OrderBy(record => record.StreamId).ThenBy(record => record.Version).Select(record => JsonSerializer.Serialize(record))];
    }
}

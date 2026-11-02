using HrAdministration.Application.Balances;
using HrAdministration.Application.EventSourcing;
using HrAdministration.Application.Leaves;
using HrAdministration.Domain.Balances;
using HrAdministration.Domain.Leaves;
using HrAdministration.Infrastructure.Projections;
using HrAdministration.Infrastructure.Tests.EventSourcing;
using SharedKernel;
using static HrAdministration.Infrastructure.Tests.EventSourcing.SampleEvents;

namespace HrAdministration.Infrastructure.Tests.Projections;

public sealed class InlineProjectionTests : IDisposable
{
    private static readonly EventMetadata Metadata = new(Actor.Employee, Origin.Manual, Guid.NewGuid());

    private static readonly LeaveBalanceId PaidLeave2026 = new(EmployeeId, BalanceType.PaidLeave, 2026);

    private readonly SqliteEventStoreDatabase _database = new();

    public void Dispose() => _database.Dispose();

    // leave_requests_view.

    [Fact]
    public async Task A_draft_appears_with_its_whole_content()
    {
        var draft = Draft(PaidLeave);

        await Append(StreamIds.LeaveRequest(draft.Id), draft);

        var view = Assert.Single(await Requests());
        Assert.Equal(draft.Id, view.LeaveRequestId);
        Assert.Equal(EmployeeId, view.EmployeeId);
        Assert.Equal(Origin.Agent, view.Origin);
        Assert.Equal(LeaveRequestStatus.Draft, view.Status);
        Assert.Equal(new LeaveTypePaidLeave(), view.Type);
        Assert.Equal(Monday, view.Start);
        Assert.Equal(Friday, view.End);
        Assert.Equal("Vacances d'été", view.Comment?.Text);
        Assert.Equal(5m, view.Duration?.Total);
        Assert.Equal(draft.Duration!.Days, view.Duration!.Days);
        Assert.Equal(draft.Flags, view.Flags);
        Assert.Empty(view.Warnings);
    }

    [Fact]
    public async Task A_draft_with_an_invalid_period_appears_without_duration_and_with_its_inconsistency()
    {
        var draft = Draft(new DraftContent(new LeaveTypePaidLeave(), Friday, Monday, null, []));

        await Append(StreamIds.LeaveRequest(draft.Id), draft);

        var view = Assert.Single(await Requests());
        Assert.Null(view.Duration);
        Assert.Null(view.Comment);
        Assert.Equal(draft.Flags, view.Flags);
        Assert.Contains(view.Flags, flag => flag.Kind == FlagKind.Inconsistency);
    }

    [Fact]
    public async Task An_update_replaces_the_whole_content()
    {
        var draft = Draft(PaidLeave);
        var streamId = StreamIds.LeaveRequest(draft.Id);
        await Append(streamId, draft);

        Assert.True(draft.Update(new DraftContent(new LeaveTypeRtt(), Monday, Monday with { HalfDay = HalfDay.Afternoon }, null, []), Today, Calendar).IsSuccess);
        await Append(streamId, draft);

        var view = Assert.Single(await Requests());
        Assert.Equal(new LeaveTypeRtt(), view.Type);
        Assert.Equal(Monday with { HalfDay = HalfDay.Afternoon }, view.End);
        Assert.Null(view.Comment);
        Assert.Equal(1m, view.Duration?.Total);
        Assert.Empty(view.Flags);
    }

    [Fact]
    public async Task A_row_added_and_changed_in_the_same_append_is_written_once()
    {
        var request = Submitted(PaidLeave);

        await Append(StreamIds.LeaveRequest(request.Id), request);

        var view = Assert.Single(await Requests());
        Assert.Equal(LeaveRequestStatus.Submitted, view.Status);
    }

    [Fact]
    public async Task A_cancelled_request_stays_in_the_view()
    {
        var draft = Draft(PaidLeave);
        Assert.True(draft.Cancel(Today).IsSuccess);

        await Append(StreamIds.LeaveRequest(draft.Id), draft);

        Assert.Equal(LeaveRequestStatus.Cancelled, Assert.Single(await Requests()).Status);
    }

    [Fact]
    public async Task A_sick_leave_override_supersedes_the_request_and_shows_what_is_left()
    {
        var request = Submitted(PaidLeave);
        Assert.True(request.Supersede().IsSuccess);
        var part = new LeavePart(LeavePeriod.Create(Monday, Monday with { HalfDay = HalfDay.Afternoon }).Value, request.Duration!);
        var carriedOver = request.CarryOver(Guid.NewGuid(), part).Value;

        Assert.True((await _database.NewProjectingStore().AppendAsync(
            [
                new StreamAppend(StreamIds.LeaveRequest(request.Id), request.Version, request.UncommittedEvents),
                new StreamAppend(StreamIds.LeaveRequest(carriedOver.Id), carriedOver.Version, carriedOver.UncommittedEvents),
            ],
            Metadata)).IsSuccess);

        var views = await Requests();
        Assert.Equal(LeaveRequestStatus.Superseded, views.Single(view => view.LeaveRequestId == request.Id).Status);
        var left = views.Single(view => view.LeaveRequestId == carriedOver.Id);
        Assert.Equal(LeaveRequestStatus.Submitted, left.Status);
        Assert.Equal(Monday, left.Start);
        Assert.Equal(request.Comment?.Text, left.Comment?.Text);
    }

    [Fact]
    public async Task A_view_says_what_the_aggregate_says()
    {
        var request = Submitted(PaidLeave);

        await Append(StreamIds.LeaveRequest(request.Id), request);

        var replayed = LeaveRequest.FromHistory(await _database.NewStore().ReadStreamAsync(StreamIds.LeaveRequest(request.Id)));
        var view = Assert.Single(await Requests());
        Assert.Equal(replayed.Status, view.Status);
        Assert.Equal(replayed.Type, view.Type);
        Assert.Equal(replayed.Start, view.Start);
        Assert.Equal(replayed.End, view.End);
        Assert.Equal(replayed.Comment, view.Comment);
        Assert.Equal(replayed.Duration!.Days, view.Duration!.Days);
        Assert.Equal(replayed.Flags, view.Flags);
        Assert.Equal(replayed.Warnings, view.Warnings);
    }

    [Fact]
    public async Task Requests_are_listed_by_first_day_for_one_employee_only()
    {
        var late = Draft(PaidLeave);
        var early = Draft(PaidLeave with { Start = new PeriodBound(new DateOnly(2026, 11, 3), HalfDay.Morning), End = new PeriodBound(new DateOnly(2026, 11, 3), HalfDay.Afternoon) });
        var other = LeaveRequest.Draft(Guid.NewGuid(), Guid.NewGuid(), Origin.Manual, PaidLeave, Today, Calendar).Value;
        await Append(StreamIds.LeaveRequest(late.Id), late);
        await Append(StreamIds.LeaveRequest(early.Id), early);
        await Append(StreamIds.LeaveRequest(other.Id), other);

        Assert.Equal([early.Id, late.Id], (await Requests()).Select(view => view.LeaveRequestId));
    }

    // balances_view.

    [Fact]
    public async Task An_entitlement_opens_a_counter()
    {
        var balance = LeaveBalance.Grant(PaidLeave2026, 25m, currentYearOnly: true);

        await Append(StreamIds.Balance(PaidLeave2026), balance);

        var view = Assert.Single(await Balances());
        Assert.Equal(PaidLeave2026, view.Id);
        Assert.Equal(25m, view.Acquired);
        Assert.True(view.CurrentYearOnly);
        Assert.Equal(25m, view.Available);
        Assert.Empty(view.Reservations);
    }

    [Fact]
    public async Task Deducted_days_split_into_taken_and_planned_on_read()
    {
        var balance = await GrantedBalance();
        Assert.True(balance.Deduct(Guid.NewGuid(), 3m, new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 4), Today).IsSuccess);
        Assert.True(balance.Deduct(Guid.NewGuid(), 5m, Monday.Date, Friday.Date, Today).IsSuccess);

        await Append(StreamIds.Balance(PaidLeave2026), balance);

        var view = Assert.Single(await Balances());
        Assert.Equal(8m, view.Reserved);
        Assert.Equal(17m, view.Available);
        Assert.Equal(3m, view.Taken(Today));
        Assert.Equal(5m, view.Planned(Today));
        Assert.Equal(8m, view.Taken(Monday.Date));
        Assert.Equal(0m, view.Planned(Monday.Date));
    }

    [Fact]
    public async Task Credited_days_give_back_what_the_request_holds()
    {
        var balance = await GrantedBalance();
        var request = Guid.NewGuid();
        Assert.True(balance.Deduct(request, 5m, Monday.Date, Friday.Date, Today).IsSuccess);
        await Append(StreamIds.Balance(PaidLeave2026), balance);

        balance.Credit(request, 5m);
        await Append(StreamIds.Balance(PaidLeave2026), balance);

        var view = Assert.Single(await Balances());
        Assert.Equal(0m, view.Reserved);
        Assert.Equal(25m, view.Available);
    }

    [Fact]
    public async Task An_override_credits_then_deducts_in_the_same_append()
    {
        var balance = await GrantedBalance();
        var superseded = Guid.NewGuid();
        Assert.True(balance.Deduct(superseded, 5m, Monday.Date, Friday.Date, Today).IsSuccess);
        await Append(StreamIds.Balance(PaidLeave2026), balance);

        balance.Credit(superseded, 5m);
        balance.DeductCarriedOver(Guid.NewGuid(), 1m, Monday.Date);
        balance.DeductCarriedOver(Guid.NewGuid(), 2m, Friday.Date);
        await Append(StreamIds.Balance(PaidLeave2026), balance);

        Assert.Equal(22m, Assert.Single(await Balances()).Available);
    }

    [Fact]
    public async Task A_view_counts_what_the_aggregate_counts()
    {
        var balance = await GrantedBalance();
        var credited = Guid.NewGuid();
        Assert.True(balance.Deduct(credited, 4m, new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 5), Today).IsSuccess);
        Assert.True(balance.Deduct(Guid.NewGuid(), 5m, Monday.Date, Friday.Date, Today).IsSuccess);
        balance.Credit(credited, 1.5m);

        await Append(StreamIds.Balance(PaidLeave2026), balance);

        var replayed = LeaveBalance.FromHistory(await _database.NewStore().ReadStreamAsync(StreamIds.Balance(PaidLeave2026)));
        var view = Assert.Single(await Balances());
        Assert.Equal(replayed.Acquired, view.Acquired);
        Assert.Equal(replayed.Reserved, view.Reserved);
        Assert.Equal(replayed.Available, view.Available);
        Assert.Equal(replayed.Taken(Today), view.Taken(Today));
        Assert.Equal(replayed.Planned(Today), view.Planned(Today));
    }

    [Fact]
    public async Task Counters_are_listed_by_year_then_type_for_one_employee_only()
    {
        var rtt2026 = new LeaveBalanceId(EmployeeId, BalanceType.Rtt, 2026);
        var paidLeave2027 = new LeaveBalanceId(EmployeeId, BalanceType.PaidLeave, 2027);
        var other = new LeaveBalanceId(Guid.NewGuid(), BalanceType.PaidLeave, 2026);
        foreach (var id in new[] { paidLeave2027, rtt2026, PaidLeave2026, other })
        {
            await Append(StreamIds.Balance(id), LeaveBalance.Grant(id, 10m, currentYearOnly: false));
        }

        Assert.Equal([PaidLeave2026, rtt2026, paidLeave2027], (await Balances()).Select(view => view.Id));
    }

    // In the transaction of the events.

    [Fact]
    public async Task A_conflict_leaves_the_views_untouched()
    {
        var balance = await GrantedBalance();
        var stale = LeaveBalance.FromHistory(await _database.NewStore().ReadStreamAsync(StreamIds.Balance(PaidLeave2026)));
        Assert.True(balance.Deduct(Guid.NewGuid(), 5m, Monday.Date, Friday.Date, Today).IsSuccess);
        await Append(StreamIds.Balance(PaidLeave2026), balance);

        Assert.True(stale.Deduct(Guid.NewGuid(), 7m, Monday.Date, Friday.Date, Today).IsSuccess);
        var result = await _database.NewProjectingStore().AppendAsync(
            [new StreamAppend(StreamIds.Balance(PaidLeave2026), stale.Version, stale.UncommittedEvents)],
            Metadata);

        Assert.Equal(EventStoreErrors.ConcurrencyConflict, result.Error);
        Assert.Equal(20m, Assert.Single(await Balances()).Available);
    }

    [Fact]
    public async Task A_conflict_on_one_stream_leaves_the_view_of_the_others_untouched()
    {
        await GrantedBalance();
        var draft = Draft(PaidLeave);
        var regranted = LeaveBalance.Grant(PaidLeave2026, 99m, currentYearOnly: false);

        var result = await _database.NewProjectingStore().AppendAsync(
            [
                new StreamAppend(StreamIds.LeaveRequest(draft.Id), draft.Version, draft.UncommittedEvents),
                new StreamAppend(StreamIds.Balance(PaidLeave2026), regranted.Version, regranted.UncommittedEvents),
            ],
            Metadata);

        Assert.Equal(EventStoreErrors.ConcurrencyConflict, result.Error);
        Assert.Empty(await Requests());
        Assert.Equal(25m, Assert.Single(await Balances()).Acquired);
    }

    [Fact]
    public async Task The_store_can_project_again_after_a_conflict()
    {
        var store = _database.NewProjectingStore();
        var balance = LeaveBalance.Grant(PaidLeave2026, 25m, currentYearOnly: false);
        var streamId = StreamIds.Balance(PaidLeave2026);
        Assert.True((await store.AppendAsync([new StreamAppend(streamId, 0, balance.UncommittedEvents)], Metadata)).IsSuccess);
        Assert.True((await store.AppendAsync([new StreamAppend(streamId, 0, balance.UncommittedEvents)], Metadata)).IsFailure);
        balance.MarkEventsAsCommitted();

        Assert.True(balance.Deduct(Guid.NewGuid(), 5m, Monday.Date, Friday.Date, Today).IsSuccess);
        Assert.True((await store.AppendAsync([new StreamAppend(streamId, balance.Version, balance.UncommittedEvents)], Metadata)).IsSuccess);

        Assert.Equal(20m, Assert.Single(await Balances()).Available);
    }

    [Fact]
    public async Task An_event_on_a_stream_without_its_first_event_is_a_bug_and_writes_nothing()
    {
        var store = _database.NewProjectingStore();

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.AppendAsync(
            [new StreamAppend(StreamIds.Balance(PaidLeave2026), 0, [new DaysCredited(Guid.NewGuid(), 1m)])],
            Metadata));

        Assert.Empty(await _database.NewStore().ReadStreamAsync(StreamIds.Balance(PaidLeave2026)));
    }

    private async Task<LeaveBalance> GrantedBalance()
    {
        var balance = LeaveBalance.Grant(PaidLeave2026, 25m, currentYearOnly: false);
        await Append(StreamIds.Balance(PaidLeave2026), balance);
        return balance;
    }

    private async Task Append<TId>(string streamId, AggregateRoot<TId> aggregate)
        where TId : notnull
    {
        var result = await _database.NewProjectingStore().AppendAsync(
            [new StreamAppend(streamId, aggregate.Version, aggregate.UncommittedEvents)],
            Metadata);
        Assert.True(result.IsSuccess, result.Error?.Code);
        aggregate.MarkEventsAsCommitted();
    }

    private Task<IReadOnlyList<LeaveRequestView>> Requests() =>
        new EfLeaveRequestViewReader(_database.NewContext()).ListAsync(EmployeeId);

    private Task<IReadOnlyList<BalanceView>> Balances() =>
        new EfBalanceViewReader(_database.NewContext()).ListAsync(EmployeeId);
}

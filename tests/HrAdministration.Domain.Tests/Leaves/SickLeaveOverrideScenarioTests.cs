using HrAdministration.Domain.Balances;
using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

/// <summary>The steps of R10 together, in the order the Application layer will run them in a single transaction.</summary>
public sealed class SickLeaveOverrideScenarioTests
{
    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly DateOnly Tuesday = new(2026, 11, 17);
    private static readonly DateOnly Wednesday = new(2026, 11, 18);
    private static readonly DateOnly Thursday = new(2026, 11, 19);
    private static readonly DateOnly Friday = new(2026, 11, 20);
    private static readonly DateOnly TheWeekBefore = new(2026, 11, 9);

    private static readonly Guid Employee = Guid.NewGuid();

    [Fact]
    public void A_sick_leave_in_the_middle_credits_the_shared_day_only_and_leaves_two_requests()
    {
        var (request, balance) = SubmittedPaidLeave(Monday, Friday, FakeLeaveCalendar.FullTime());

        var parts = Override(request, balance, sickFrom: Wednesday, sickTo: Wednesday, FakeLeaveCalendar.FullTime());

        Assert.Equal(LeaveRequestStatus.Superseded, request.Status);
        Assert.All(parts, part => Assert.Equal(LeaveRequestStatus.Submitted, part.Status));
        Assert.Equal([Monday, Thursday], parts.Select(part => part.Start.Date));
        Assert.Equal(4m, balance.Reserved);
        Assert.Equal(21m, balance.Available);
    }

    [Fact]
    public void Each_remaining_part_is_taken_or_planned_on_its_own_first_day()
    {
        var (request, balance) = SubmittedPaidLeave(Monday, Friday, FakeLeaveCalendar.FullTime());

        Override(request, balance, sickFrom: Wednesday, sickTo: Wednesday, FakeLeaveCalendar.FullTime());

        Assert.Equal(2m, balance.Taken(Wednesday));
        Assert.Equal(2m, balance.Planned(Wednesday));
    }

    [Fact]
    public void A_request_losing_its_start_is_planned_until_its_new_first_day()
    {
        var (request, balance) = SubmittedPaidLeave(Monday, Friday, FakeLeaveCalendar.FullTime());

        Override(request, balance, sickFrom: Monday, sickTo: Tuesday, FakeLeaveCalendar.FullTime());

        Assert.Equal(0m, balance.Taken(Tuesday));
        Assert.Equal(3m, balance.Planned(Tuesday));
        Assert.Equal(3m, balance.Taken(Wednesday));
    }

    [Fact]
    public void A_request_covered_entirely_leaves_nothing_reserved()
    {
        var (request, balance) = SubmittedPaidLeave(Monday, Friday, FakeLeaveCalendar.FullTime());

        var parts = Override(request, balance, sickFrom: Monday, sickTo: Friday, FakeLeaveCalendar.FullTime());

        Assert.Empty(parts);
        Assert.Equal(0m, balance.Reserved);
        Assert.Equal(25m, balance.Available);
    }

    [Fact]
    public void A_whole_balance_used_by_the_request_can_still_be_overridden()
    {
        var (request, balance) = SubmittedPaidLeave(Monday, Friday, FakeLeaveCalendar.FullTime(), entitlement: 5m);

        Override(request, balance, sickFrom: Wednesday, sickTo: Wednesday, FakeLeaveCalendar.FullTime());

        Assert.Equal(1m, balance.Available);
    }

    [Fact]
    public void A_shared_non_working_day_can_move_the_first_day_without_crediting_anything()
    {
        var calendar = FakeLeaveCalendar.PartTime();
        var (request, balance) = SubmittedPaidLeave(Wednesday, Thursday, calendar);

        Override(request, balance, sickFrom: Tuesday, sickTo: Wednesday, calendar);

        Assert.Equal(1m, balance.Reserved);
        Assert.Equal(0m, balance.Taken(Wednesday));
        Assert.Equal(1m, balance.Taken(Thursday));
    }

    [Fact]
    public void Another_sick_leave_on_a_remaining_part_overrides_it_like_any_request()
    {
        var calendar = FakeLeaveCalendar.FullTime();
        var (request, balance) = SubmittedPaidLeave(Monday, Friday, calendar);
        var parts = Override(request, balance, sickFrom: Wednesday, sickTo: Wednesday, calendar);

        var remaining = Override(parts[1], balance, sickFrom: Friday, sickTo: Friday, calendar);

        Assert.Equal(LeaveRequestStatus.Superseded, parts[1].Status);
        Assert.Equal(Thursday, Assert.Single(remaining).End.Date);
        Assert.Equal(3m, balance.Reserved);
    }

    private static (LeaveRequest Request, LeaveBalance Balance) SubmittedPaidLeave(
        DateOnly first,
        DateOnly last,
        ILeaveCalendar calendar,
        decimal entitlement = 25m)
    {
        var content = new DraftContent(new LeaveTypePaidLeave(), new PeriodBound(first, HalfDay.Morning), new PeriodBound(last, HalfDay.Afternoon), null, []);
        var request = LeaveRequest.Draft(Guid.NewGuid(), Employee, Origin.Manual, content, TheWeekBefore, calendar).Value;
        request.Submit(TheWeekBefore, calendar);

        var balance = LeaveBalance.Grant(new LeaveBalanceId(Employee, BalanceType.PaidLeave, first.Year), entitlement, currentYearOnly: false);
        balance.Deduct(request.Id, request.Duration!.Total, first, last, TheWeekBefore);

        return (request, balance);
    }

    private static List<LeaveRequest> Override(LeaveRequest request, LeaveBalance balance, DateOnly sickFrom, DateOnly sickTo, ILeaveCalendar calendar)
    {
        var sickLeave = LeavePeriod.Create(new PeriodBound(sickFrom, HalfDay.Morning), new PeriodBound(sickTo, HalfDay.Afternoon)).Value;
        var leave = new OverridableLeave(request.Id, request.Type, request.Status, request.Start, request.End, request.Duration!.Total);

        var leaveOverride = Assert.Single(SickLeaveOverridePolicy.Plan(sickLeave, [leave], calendar).Value);

        request.Supersede();
        balance.Credit(request.Id, request.Duration.Total);

        var parts = new List<LeaveRequest>();
        foreach (var part in leaveOverride.RemainingParts)
        {
            var carriedOver = request.CarryOver(Guid.NewGuid(), part).Value;
            balance.DeductCarriedOver(carriedOver.Id, part.Duration.Total, part.Period.Start.Date);
            parts.Add(carriedOver);
        }

        return parts;
    }
}

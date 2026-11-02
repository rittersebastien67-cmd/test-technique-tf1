using HrAdministration.Domain.Balances;
using HrAdministration.Domain.Leaves;
using SharedKernel;

namespace HrAdministration.Infrastructure.Tests.EventSourcing;

/// <summary>Events raised by the real aggregates, so that the tests store what the application will store.</summary>
internal static class SampleEvents
{
    public static readonly Guid EmployeeId = Guid.NewGuid();

    public static readonly DateOnly Today = new(2026, 11, 2);

    public static readonly PeriodBound Monday = new(new DateOnly(2026, 11, 9), HalfDay.Morning);

    public static readonly PeriodBound Friday = new(new DateOnly(2026, 11, 13), HalfDay.Afternoon);

    public static DraftContent PaidLeave => new(new LeaveTypePaidLeave(), Monday, Friday, "Vacances d'été", ["YEAR_INFERRED"]);

    public static LeaveRequest Draft(DraftContent content) =>
        LeaveRequest.Draft(Guid.NewGuid(), EmployeeId, Origin.Agent, content, Today, new WeekdaysCalendar()).Value;

    public static LeaveRequest Submitted(DraftContent content)
    {
        var request = Draft(content);
        Assert.True(request.Submit(Today, new WeekdaysCalendar()).IsSuccess);
        return request;
    }

    public static IReadOnlyList<IDomainEvent> OfEveryType()
    {
        var balance = LeaveBalance.Grant(new LeaveBalanceId(EmployeeId, BalanceType.PaidLeave, 2026), 25m, currentYearOnly: true);
        Assert.True(balance.Deduct(Guid.NewGuid(), 5m, Monday.Date, Friday.Date, Today).IsSuccess);

        var deductedRequest = Guid.NewGuid();
        Assert.True(balance.Deduct(deductedRequest, 2.5m, Monday.Date, Friday.Date, Today).IsSuccess);
        balance.Credit(deductedRequest, 2.5m);

        var superseded = Draft(PaidLeave);
        Assert.True(superseded.Update(PaidLeave with { Comment = null }, Today, new WeekdaysCalendar()).IsSuccess);
        Assert.True(superseded.Submit(Today, new WeekdaysCalendar()).IsSuccess);
        Assert.True(superseded.Supersede().IsSuccess);

        var part = new LeavePart(LeavePeriod.Create(Monday, Friday).Value, superseded.Duration!);
        var carriedOver = superseded.CarryOver(Guid.NewGuid(), part).Value;

        var cancelled = Draft(PaidLeave);
        Assert.True(cancelled.Cancel(Today).IsSuccess);

        return [.. balance.UncommittedEvents, .. superseded.UncommittedEvents, .. carriedOver.UncommittedEvents, .. cancelled.UncommittedEvents];
    }

    private sealed class WeekdaysCalendar : ILeaveCalendar
    {
        public bool Covers(DateOnly date) => true;

        public DayKind Qualify(DateOnly date) =>
            date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? DayKind.Weekend : DayKind.WorkingDay;
    }
}

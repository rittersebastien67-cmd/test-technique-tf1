using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

public sealed class SickLeaveOverridePolicyTests
{
    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly DateOnly Tuesday = new(2026, 11, 17);
    private static readonly DateOnly Wednesday = new(2026, 11, 18);
    private static readonly DateOnly Thursday = new(2026, 11, 19);
    private static readonly DateOnly Friday = new(2026, 11, 20);
    private static readonly DateOnly Saturday = new(2026, 11, 21);
    private static readonly DateOnly Sunday = new(2026, 11, 22);
    private static readonly DateOnly NextMonday = new(2026, 11, 23);

    private static readonly Guid Request = Guid.NewGuid();
    private static readonly Guid OtherRequest = Guid.NewGuid();

    private static readonly ILeaveCalendar FullTime = FakeLeaveCalendar.FullTime();
    private static readonly ILeaveCalendar PartTime = FakeLeaveCalendar.PartTime();

    private static readonly LeavePeriod Week = FullDays(Monday, Friday);

    // Nothing to override.

    [Fact]
    public void A_sick_leave_without_any_other_request_overrides_nothing()
    {
        var result = SickLeaveOverridePolicy.Plan(Week, [], FullTime);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void A_request_that_is_not_overlapped_is_left_alone()
    {
        var result = SickLeaveOverridePolicy.Plan(FullDays(Thursday, Friday), [Submitted(FullDays(Monday, Tuesday), 2m)], FullTime);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void The_morning_and_the_afternoon_of_a_same_day_do_not_override_each_other()
    {
        var morning = Period(Monday, HalfDay.Morning, Monday, HalfDay.Morning);
        var afternoon = Period(Monday, HalfDay.Afternoon, Monday, HalfDay.Afternoon);

        var result = SickLeaveOverridePolicy.Plan(morning, [Submitted(afternoon, 0.5m)], FullTime);

        Assert.Empty(result.Value);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Draft)]
    [InlineData(LeaveRequestStatus.Cancelled)]
    [InlineData(LeaveRequestStatus.Superseded)]
    public void Only_submitted_requests_are_overridden(LeaveRequestStatus status)
    {
        var leave = new OverridableLeave(Request, new LeaveTypePaidLeave(), status, Week.Start, Week.End, 5m);

        var result = SickLeaveOverridePolicy.Plan(Week, [leave], FullTime);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void A_draft_with_an_invalid_period_is_ignored()
    {
        var invalidDraft = new OverridableLeave(
            Request,
            new LeaveTypePaidLeave(),
            LeaveRequestStatus.Draft,
            Week.End,
            Week.Start,
            0m);

        var result = SickLeaveOverridePolicy.Plan(Week, [invalidDraft], FullTime);

        Assert.Empty(result.Value);
    }

    // Nothing left of the request.

    [Fact]
    public void A_request_covered_entirely_leaves_no_part_and_all_its_days_are_credited()
    {
        var result = SickLeaveOverridePolicy.Plan(Week, [Submitted(FullDays(Tuesday, Thursday), 3m)], FullTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Equal(Request, leaveOverride.LeaveRequestId);
        Assert.Empty(leaveOverride.RemainingParts);
        Assert.Equal(3m, leaveOverride.DaysRemoved);
        Assert.Equal(3m, leaveOverride.DaysCredited);
        Assert.Equal(FullDays(Tuesday, Thursday), leaveOverride.Removed);
    }

    [Fact]
    public void A_request_with_the_same_period_leaves_no_part()
    {
        var result = SickLeaveOverridePolicy.Plan(Week, [Submitted(Week, 5m)], FullTime);

        Assert.Empty(Assert.Single(result.Value).RemainingParts);
    }

    [Fact]
    public void Days_that_do_not_count_do_not_make_a_part()
    {
        var result = SickLeaveOverridePolicy.Plan(
            FullDays(NextMonday, NextMonday),
            [Submitted(FullDays(Saturday, NextMonday), 1m, new LeaveTypeRtt())],
            FullTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Empty(leaveOverride.RemainingParts);
        Assert.Equal(1m, leaveOverride.DaysCredited);
    }

    // One part left.

    [Fact]
    public void A_sick_leave_on_the_end_of_a_request_leaves_the_part_before_it()
    {
        var result = SickLeaveOverridePolicy.Plan(FullDays(Thursday, Friday), [Submitted(Week, 5m)], FullTime);

        var leaveOverride = Assert.Single(result.Value);
        var part = Assert.Single(leaveOverride.RemainingParts);
        Assert.Equal(FullDays(Monday, Wednesday), part.Period);
        Assert.Equal(3m, part.Duration.Total);
        Assert.Equal(2m, leaveOverride.DaysCredited);
        Assert.Equal(FullDays(Thursday, Friday), leaveOverride.Removed);
    }

    [Fact]
    public void A_sick_leave_on_the_start_of_a_request_leaves_the_part_after_it()
    {
        var result = SickLeaveOverridePolicy.Plan(FullDays(Monday, Tuesday), [Submitted(Week, 5m)], FullTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Equal(FullDays(Wednesday, Friday), Assert.Single(leaveOverride.RemainingParts).Period);
        Assert.Equal(2m, leaveOverride.DaysCredited);
    }

    [Fact]
    public void A_sick_leave_going_past_a_request_only_removes_the_shared_half_days()
    {
        var leave = Submitted(FullDays(Monday, Wednesday), 3m);

        var result = SickLeaveOverridePolicy.Plan(FullDays(Wednesday, Friday), [leave], FullTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Equal(FullDays(Wednesday, Wednesday), leaveOverride.Removed);
        Assert.Equal(1m, leaveOverride.DaysCredited);
    }

    [Fact]
    public void A_sick_leave_from_the_afternoon_keeps_the_morning()
    {
        var result = SickLeaveOverridePolicy.Plan(
            Period(Wednesday, HalfDay.Afternoon, Friday, HalfDay.Afternoon),
            [Submitted(Week, 5m)],
            FullTime);

        var leaveOverride = Assert.Single(result.Value);
        var part = Assert.Single(leaveOverride.RemainingParts);
        Assert.Equal(Period(Monday, HalfDay.Morning, Wednesday, HalfDay.Morning), part.Period);
        Assert.Equal(2.5m, part.Duration.Total);
        Assert.Equal(2.5m, leaveOverride.DaysCredited);
    }

    [Fact]
    public void A_sick_leave_until_noon_keeps_the_afternoon()
    {
        var result = SickLeaveOverridePolicy.Plan(
            Period(Monday, HalfDay.Morning, Wednesday, HalfDay.Morning),
            [Submitted(Week, 5m)],
            FullTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Equal(Period(Wednesday, HalfDay.Afternoon, Friday, HalfDay.Afternoon), Assert.Single(leaveOverride.RemainingParts).Period);
        Assert.Equal(2.5m, leaveOverride.DaysCredited);
    }

    [Fact]
    public void A_sick_leave_can_take_half_of_a_single_day_leave()
    {
        var afternoon = Period(Monday, HalfDay.Afternoon, Monday, HalfDay.Afternoon);

        var result = SickLeaveOverridePolicy.Plan(afternoon, [Submitted(FullDays(Monday, Monday), 1m)], FullTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Equal(Period(Monday, HalfDay.Morning, Monday, HalfDay.Morning), Assert.Single(leaveOverride.RemainingParts).Period);
        Assert.Equal(0.5m, leaveOverride.DaysCredited);
    }

    [Fact]
    public void A_side_left_with_days_that_do_not_count_makes_no_part()
    {
        var result = SickLeaveOverridePolicy.Plan(
            FullDays(Friday, Friday),
            [Submitted(FullDays(Thursday, Sunday), 2m, new LeaveTypeRtt())],
            FullTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Equal(FullDays(Thursday, Thursday), Assert.Single(leaveOverride.RemainingParts).Period);
        Assert.Equal(1m, leaveOverride.DaysCredited);
    }

    // Two parts left.

    [Fact]
    public void A_sick_leave_in_the_middle_of_a_request_leaves_a_part_on_each_side_in_chronological_order()
    {
        var result = SickLeaveOverridePolicy.Plan(FullDays(Wednesday, Wednesday), [Submitted(Week, 5m)], FullTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Equal(2, leaveOverride.RemainingParts.Count);
        Assert.Equal(FullDays(Monday, Tuesday), leaveOverride.RemainingParts[0].Period);
        Assert.Equal(2m, leaveOverride.RemainingParts[0].Duration.Total);
        Assert.Equal(FullDays(Thursday, Friday), leaveOverride.RemainingParts[1].Period);
        Assert.Equal(2m, leaveOverride.RemainingParts[1].Duration.Total);
        Assert.Equal(1m, leaveOverride.DaysCredited);
    }

    [Fact]
    public void A_sick_leave_of_half_a_day_in_the_middle_of_a_request_cuts_it_at_the_half_day()
    {
        var result = SickLeaveOverridePolicy.Plan(
            Period(Wednesday, HalfDay.Morning, Wednesday, HalfDay.Morning),
            [Submitted(Week, 5m)],
            FullTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Equal(FullDays(Monday, Tuesday), leaveOverride.RemainingParts[0].Period);
        Assert.Equal(Period(Wednesday, HalfDay.Afternoon, Friday, HalfDay.Afternoon), leaveOverride.RemainingParts[1].Period);
        Assert.Equal(2.5m, leaveOverride.RemainingParts[1].Duration.Total);
        Assert.Equal(0.5m, leaveOverride.DaysCredited);
    }

    // Several requests.

    [Fact]
    public void Each_overlapped_request_has_its_own_override_in_chronological_order()
    {
        var early = Submitted(FullDays(Monday, Tuesday), 2m);
        var late = Submitted(FullDays(Thursday, Friday), 2m, new LeaveTypeRtt()) with { LeaveRequestId = OtherRequest };

        var expected = new[] { Request, OtherRequest };
        var sickLeave = FullDays(Tuesday, Thursday);
        Assert.Equal(expected, SickLeaveOverridePolicy.Plan(sickLeave, [early, late], FullTime).Value.Select(o => o.LeaveRequestId));
        Assert.Equal(expected, SickLeaveOverridePolicy.Plan(sickLeave, [late, early], FullTime).Value.Select(o => o.LeaveRequestId));
    }

    // Types.

    [Fact]
    public void Days_of_a_type_with_a_counter_are_credited()
    {
        Assert.Equal(5m, Assert.Single(SickLeaveOverridePolicy.Plan(Week, [Submitted(Week, 5m, new LeaveTypePaidLeave())], FullTime).Value).DaysCredited);
        Assert.Equal(5m, Assert.Single(SickLeaveOverridePolicy.Plan(Week, [Submitted(Week, 5m, new LeaveTypeRtt())], FullTime).Value).DaysCredited);
    }

    [Fact]
    public void Unpaid_leave_is_overridden_but_has_no_counter_to_credit()
    {
        var result = SickLeaveOverridePolicy.Plan(Week, [Submitted(Week, 5m, new LeaveTypeUnpaidLeave())], FullTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Equal(5m, leaveOverride.DaysRemoved);
        Assert.Equal(0m, leaveOverride.DaysCredited);
    }

    [Fact]
    public void Two_sick_leaves_stay_incompatible()
    {
        var result = SickLeaveOverridePolicy.Plan(
            FullDays(Tuesday, Tuesday),
            [Submitted(FullDays(Monday, Wednesday), 3m, new LeaveTypeSickLeave())],
            FullTime);

        Assert.Equal("period.overlap", result.Error?.Code);
        Assert.Equal("Chevauche la demande du 16/11 au 18/11.", result.Error?.Message);
    }

    [Fact]
    public void Two_sick_leaves_can_share_a_day_on_different_half_days()
    {
        var morning = Period(Monday, HalfDay.Morning, Monday, HalfDay.Morning);
        var afternoon = Period(Monday, HalfDay.Afternoon, Monday, HalfDay.Afternoon);

        var result = SickLeaveOverridePolicy.Plan(afternoon, [Submitted(morning, 0.5m, new LeaveTypeSickLeave())], FullTime);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Another_sick_leave_refuses_the_whole_plan_even_if_a_leave_could_be_overridden()
    {
        var result = SickLeaveOverridePolicy.Plan(
            Week,
            [Submitted(FullDays(Monday, Tuesday), 2m), Submitted(FullDays(Thursday, Friday), 2m, new LeaveTypeSickLeave())],
            FullTime);

        Assert.Equal("period.overlap", result.Error?.Code);
    }

    // Part time (PRD 4.1): Wednesday is not worked, and paid leave counts it after a full worked day.

    [Fact]
    public void Part_time_paid_leave_is_counted_again_with_its_non_working_days()
    {
        var result = SickLeaveOverridePolicy.Plan(FullDays(Thursday, Friday), [Submitted(Week, 5m)], PartTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Equal(3m, Assert.Single(leaveOverride.RemainingParts).Duration.Total);
        Assert.Equal(2m, leaveOverride.DaysCredited);
    }

    [Fact]
    public void A_non_working_day_follows_the_worked_day_it_depends_on()
    {
        var result = SickLeaveOverridePolicy.Plan(FullDays(Tuesday, Tuesday), [Submitted(Week, 5m)], PartTime);

        var leaveOverride = Assert.Single(result.Value);
        Assert.Equal(1m, leaveOverride.RemainingParts[0].Duration.Total);
        Assert.Equal(2m, leaveOverride.RemainingParts[1].Duration.Total);
        Assert.Equal(2m, leaveOverride.DaysCredited);
    }

    [Fact]
    public void Taking_only_a_non_working_day_credits_nothing_as_the_day_before_still_counts_it()
    {
        var result = SickLeaveOverridePolicy.Plan(
            Period(Wednesday, HalfDay.Morning, Thursday, HalfDay.Afternoon),
            [Submitted(FullDays(Tuesday, Wednesday), 2m)],
            PartTime);

        var leaveOverride = Assert.Single(result.Value);
        var part = Assert.Single(leaveOverride.RemainingParts);
        Assert.Equal(FullDays(Tuesday, Tuesday), part.Period);
        Assert.Equal(2m, part.Duration.Total);
        Assert.Equal(0m, leaveOverride.DaysCredited);
    }

    // Other errors of the count.

    [Fact]
    public void A_part_outside_the_calendar_is_refused()
    {
        var result = SickLeaveOverridePolicy.Plan(
            FullDays(Monday, Monday),
            [Submitted(FullDays(Monday, Wednesday), 3m)],
            FakeLeaveCalendar.FullTimeUntil(Tuesday));

        Assert.Equal("period.outside_calendar", result.Error?.Code);
    }

    private static LeavePeriod FullDays(DateOnly start, DateOnly end) => Period(start, HalfDay.Morning, end, HalfDay.Afternoon);

    private static LeavePeriod Period(DateOnly start, HalfDay startHalfDay, DateOnly end, HalfDay endHalfDay) =>
        LeavePeriod.Create(new PeriodBound(start, startHalfDay), new PeriodBound(end, endHalfDay)).Value;

    private static OverridableLeave Submitted(LeavePeriod period, decimal days, LeaveType? type = null) =>
        new(Request, type ?? new LeaveTypePaidLeave(), LeaveRequestStatus.Submitted, period.Start, period.End, days);
}

using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

public sealed class OverlapPolicyTests
{
    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly DateOnly Tuesday = new(2026, 11, 17);
    private static readonly DateOnly Wednesday = new(2026, 11, 18);
    private static readonly DateOnly Thursday = new(2026, 11, 19);
    private static readonly DateOnly Friday = new(2026, 11, 20);

    [Fact]
    public void A_period_is_accepted_when_the_employee_has_no_other_request()
    {
        var result = OverlapPolicy.Check(Period(Monday, HalfDay.Morning, Friday, HalfDay.Afternoon), []);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void The_morning_and_the_afternoon_of_a_same_day_are_compatible()
    {
        var morning = Period(Monday, HalfDay.Morning, Monday, HalfDay.Morning);
        var afternoon = Period(Monday, HalfDay.Afternoon, Monday, HalfDay.Afternoon);

        Assert.True(OverlapPolicy.Check(morning, [Submitted(afternoon)]).IsSuccess);
        Assert.True(OverlapPolicy.Check(afternoon, [Submitted(morning)]).IsSuccess);
    }

    [Fact]
    public void A_period_can_start_on_the_afternoon_where_another_stops_at_noon()
    {
        var untilTuesdayNoon = Period(Monday, HalfDay.Morning, Tuesday, HalfDay.Morning);
        var fromTuesdayAfternoon = Period(Tuesday, HalfDay.Afternoon, Wednesday, HalfDay.Afternoon);

        Assert.True(OverlapPolicy.Check(fromTuesdayAfternoon, [Submitted(untilTuesdayNoon)]).IsSuccess);
        Assert.True(OverlapPolicy.Check(untilTuesdayNoon, [Submitted(fromTuesdayAfternoon)]).IsSuccess);
    }

    [Fact]
    public void Periods_on_consecutive_days_are_compatible()
    {
        var mondayAndTuesday = Period(Monday, HalfDay.Morning, Tuesday, HalfDay.Afternoon);
        var wednesdayAndThursday = Period(Wednesday, HalfDay.Morning, Thursday, HalfDay.Afternoon);

        Assert.True(OverlapPolicy.Check(mondayAndTuesday, [Submitted(wednesdayAndThursday)]).IsSuccess);
        Assert.True(OverlapPolicy.Check(wednesdayAndThursday, [Submitted(mondayAndTuesday)]).IsSuccess);
    }

    [Theory]
    [InlineData(HalfDay.Morning, HalfDay.Morning)]
    [InlineData(HalfDay.Afternoon, HalfDay.Afternoon)]
    [InlineData(HalfDay.Morning, HalfDay.Afternoon)]
    public void Sharing_the_same_half_day_is_refused(HalfDay start, HalfDay end)
    {
        var period = Period(Monday, start, Monday, end);

        var result = OverlapPolicy.Check(period, [Submitted(period)]);

        Assert.Equal("period.overlap", result.Error?.Code);
    }

    [Fact]
    public void A_full_day_overlaps_each_of_its_half_days()
    {
        var fullDay = Period(Monday, HalfDay.Morning, Monday, HalfDay.Afternoon);
        var morning = Period(Monday, HalfDay.Morning, Monday, HalfDay.Morning);
        var afternoon = Period(Monday, HalfDay.Afternoon, Monday, HalfDay.Afternoon);

        Assert.Equal("period.overlap", OverlapPolicy.Check(fullDay, [Submitted(morning)]).Error?.Code);
        Assert.Equal("period.overlap", OverlapPolicy.Check(fullDay, [Submitted(afternoon)]).Error?.Code);
        Assert.Equal("period.overlap", OverlapPolicy.Check(morning, [Submitted(fullDay)]).Error?.Code);
        Assert.Equal("period.overlap", OverlapPolicy.Check(afternoon, [Submitted(fullDay)]).Error?.Code);
    }

    [Fact]
    public void Periods_sharing_a_single_half_day_at_their_ends_overlap()
    {
        var untilTuesdayAfternoon = Period(Monday, HalfDay.Morning, Tuesday, HalfDay.Afternoon);
        var fromTuesdayAfternoon = Period(Tuesday, HalfDay.Afternoon, Wednesday, HalfDay.Afternoon);

        Assert.Equal("period.overlap", OverlapPolicy.Check(untilTuesdayAfternoon, [Submitted(fromTuesdayAfternoon)]).Error?.Code);
        Assert.Equal("period.overlap", OverlapPolicy.Check(fromTuesdayAfternoon, [Submitted(untilTuesdayAfternoon)]).Error?.Code);
    }

    [Fact]
    public void A_period_inside_another_overlaps_it_both_ways()
    {
        var week = Period(Monday, HalfDay.Morning, Friday, HalfDay.Afternoon);
        var wednesday = Period(Wednesday, HalfDay.Morning, Wednesday, HalfDay.Afternoon);

        Assert.Equal("period.overlap", OverlapPolicy.Check(week, [Submitted(wednesday)]).Error?.Code);
        Assert.Equal("period.overlap", OverlapPolicy.Check(wednesday, [Submitted(week)]).Error?.Code);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Draft)]
    [InlineData(LeaveRequestStatus.Cancelled)]
    [InlineData(LeaveRequestStatus.Superseded)]
    public void Only_submitted_requests_count(LeaveRequestStatus status)
    {
        var period = Period(Monday, HalfDay.Morning, Friday, HalfDay.Afternoon);

        var result = OverlapPolicy.Check(period, [new ExistingLeave(status, period.Start, period.End)]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void A_draft_with_an_invalid_period_is_ignored()
    {
        var invalidDraft = new ExistingLeave(
            LeaveRequestStatus.Draft,
            new PeriodBound(Friday, HalfDay.Afternoon),
            new PeriodBound(Monday, HalfDay.Morning));

        var result = OverlapPolicy.Check(Period(Tuesday, HalfDay.Morning, Wednesday, HalfDay.Afternoon), [invalidDraft]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void An_ignored_request_does_not_hide_a_submitted_one()
    {
        var period = Period(Monday, HalfDay.Morning, Friday, HalfDay.Afternoon);
        var cancelled = new ExistingLeave(LeaveRequestStatus.Cancelled, period.Start, period.End);

        var result = OverlapPolicy.Check(period, [cancelled, Submitted(period)]);

        Assert.Equal("period.overlap", result.Error?.Code);
    }

    [Fact]
    public void The_error_names_the_overlapped_request()
    {
        var result = OverlapPolicy.Check(
            Period(Tuesday, HalfDay.Morning, Tuesday, HalfDay.Afternoon),
            [Submitted(Period(Monday, HalfDay.Afternoon, Wednesday, HalfDay.Morning))]);

        Assert.Equal("Chevauche la demande du 16/11 au 18/11.", result.Error?.Message);
    }

    [Fact]
    public void The_error_names_the_earliest_request_whatever_the_order_of_the_list()
    {
        var earliest = Submitted(Period(Tuesday, HalfDay.Morning, Tuesday, HalfDay.Afternoon));
        var latest = Submitted(Period(Thursday, HalfDay.Morning, Thursday, HalfDay.Afternoon));
        var week = Period(Monday, HalfDay.Morning, Friday, HalfDay.Afternoon);

        const string Expected = "Chevauche la demande du 17/11 au 17/11.";
        Assert.Equal(Expected, OverlapPolicy.Check(week, [earliest, latest]).Error?.Message);
        Assert.Equal(Expected, OverlapPolicy.Check(week, [latest, earliest]).Error?.Message);
    }

    private static LeavePeriod Period(DateOnly start, HalfDay startHalfDay, DateOnly end, HalfDay endHalfDay) =>
        LeavePeriod.Create(new PeriodBound(start, startHalfDay), new PeriodBound(end, endHalfDay)).Value;

    private static ExistingLeave Submitted(LeavePeriod period) =>
        new(LeaveRequestStatus.Submitted, period.Start, period.End);
}

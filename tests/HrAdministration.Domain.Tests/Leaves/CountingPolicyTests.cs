using HrAdministration.Domain.Leaves;
using SharedKernel;

namespace HrAdministration.Domain.Tests.Leaves;

public sealed class CountingPolicyTests
{
    private static readonly ILeaveCalendar FullTime = FakeLeaveCalendar.FullTime();
    private static readonly ILeaveCalendar PartTime = FakeLeaveCalendar.PartTime();

    // A week of November 2026.
    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly DateOnly Tuesday = new(2026, 11, 17);
    private static readonly DateOnly Wednesday = new(2026, 11, 18);
    private static readonly DateOnly Thursday = new(2026, 11, 19);
    private static readonly DateOnly Friday = new(2026, 11, 20);
    private static readonly DateOnly Saturday = new(2026, 11, 21);
    private static readonly DateOnly Sunday = new(2026, 11, 22);
    private static readonly DateOnly NextMonday = new(2026, 11, 23);

    // PRD table 4.1, line by line.

    [Fact]
    public void Part_time_paid_leave_from_monday_to_friday_is_5_days()
    {
        Assert.Equal(5m, Total(new LeaveTypePaidLeave(), PartTime, Monday, Friday));
    }

    [Fact]
    public void Part_time_paid_leave_on_tuesday_alone_is_2_days()
    {
        Assert.Equal(2m, Total(new LeaveTypePaidLeave(), PartTime, Tuesday, Tuesday));
    }

    [Fact]
    public void Part_time_paid_leave_from_wednesday_to_friday_is_2_days()
    {
        Assert.Equal(2m, Total(new LeaveTypePaidLeave(), PartTime, Wednesday, Friday));
    }

    [Fact]
    public void Part_time_paid_leave_on_wednesday_alone_is_invalid()
    {
        Assert.Equal("duration.zero", Count(new LeaveTypePaidLeave(), PartTime, Wednesday, Wednesday).Error?.Code);
    }

    [Fact]
    public void Part_time_paid_leave_on_tuesday_afternoon_is_half_a_day()
    {
        var result = Count(new LeaveTypePaidLeave(), PartTime, Tuesday, Tuesday, HalfDay.Afternoon, HalfDay.Afternoon);

        Assert.Equal(0.5m, result.Value.Total);
    }

    // Legal count of paid leave, beyond the table.

    [Fact]
    public void Part_time_paid_leave_on_tuesday_morning_is_half_a_day()
    {
        var result = Count(new LeaveTypePaidLeave(), PartTime, Tuesday, Tuesday, HalfDay.Morning, HalfDay.Morning);

        Assert.Equal(0.5m, result.Value.Total);
    }

    [Fact]
    public void Part_time_paid_leave_ending_on_tuesday_morning_does_not_count_wednesday()
    {
        var result = Count(new LeaveTypePaidLeave(), PartTime, Monday, Tuesday, HalfDay.Morning, HalfDay.Morning);

        Assert.Equal(1.5m, result.Value.Total);
    }

    [Fact]
    public void Part_time_paid_leave_starting_on_tuesday_afternoon_does_not_count_wednesday()
    {
        var result = Count(new LeaveTypePaidLeave(), PartTime, Tuesday, Thursday, HalfDay.Afternoon, HalfDay.Afternoon);

        Assert.Equal(1.5m, result.Value.Total);
    }

    [Fact]
    public void Part_time_paid_leave_ending_on_wednesday_morning_counts_wednesday_in_full()
    {
        var result = Count(new LeaveTypePaidLeave(), PartTime, Tuesday, Wednesday, HalfDay.Morning, HalfDay.Morning);

        Assert.Equal(2m, result.Value.Total);
    }

    [Theory]
    [InlineData(2026, 11, 16)] // Monday: the return to work is on Tuesday.
    [InlineData(2026, 11, 19)] // Thursday: the return to work is on Friday.
    [InlineData(2026, 11, 20)] // Friday: the return to work is on Monday, and the weekend does not count.
    public void Part_time_paid_leave_before_a_worked_day_is_1_day(int year, int month, int day)
    {
        var date = new DateOnly(year, month, day);

        Assert.Equal(1m, Total(new LeaveTypePaidLeave(), PartTime, date, date));
    }

    [Fact]
    public void Part_time_paid_leave_over_two_weeks_counts_both_wednesdays_but_no_weekend()
    {
        Assert.Equal(10m, Total(new LeaveTypePaidLeave(), PartTime, Monday, new DateOnly(2026, 11, 27)));
    }

    [Fact]
    public void A_public_holiday_on_the_non_working_day_does_not_count()
    {
        var calendar = FakeLeaveCalendar.PartTime(Wednesday);

        Assert.Equal(1m, Total(new LeaveTypePaidLeave(), calendar, Tuesday, Tuesday));
    }

    [Fact]
    public void A_public_holiday_before_the_non_working_day_does_not_stop_the_legal_count()
    {
        // The return to work is on Thursday, so Wednesday counts.
        var calendar = FakeLeaveCalendar.PartTime(Tuesday);

        Assert.Equal(2m, Total(new LeaveTypePaidLeave(), calendar, Monday, Monday));
    }

    [Fact]
    public void A_public_holiday_after_the_non_working_day_does_not_count()
    {
        var calendar = FakeLeaveCalendar.PartTime(Thursday);

        Assert.Equal(2m, Total(new LeaveTypePaidLeave(), calendar, Tuesday, Tuesday));
    }

    [Fact]
    public void Part_time_paid_leave_on_a_holiday_and_the_next_non_working_day_is_invalid()
    {
        var calendar = FakeLeaveCalendar.PartTime(Tuesday);

        Assert.Equal("duration.zero", Count(new LeaveTypePaidLeave(), calendar, Tuesday, Wednesday).Error?.Code);
    }

    // Worked days only (PRD D39).

    public static TheoryData<string> TypesCountingWorkedDaysOnly => new("SickLeave", "UnpaidLeave", "Rtt");

    [Theory]
    [MemberData(nameof(TypesCountingWorkedDaysOnly))]
    public void Part_time_monday_to_friday_is_4_worked_days(string typeName)
    {
        Assert.Equal(4m, Total(TypeNamed(typeName), PartTime, Monday, Friday));
    }

    [Theory]
    [MemberData(nameof(TypesCountingWorkedDaysOnly))]
    public void Part_time_tuesday_alone_is_1_worked_day(string typeName)
    {
        Assert.Equal(1m, Total(TypeNamed(typeName), PartTime, Tuesday, Tuesday));
    }

    [Theory]
    [MemberData(nameof(TypesCountingWorkedDaysOnly))]
    public void Part_time_wednesday_alone_is_invalid(string typeName)
    {
        Assert.Equal("duration.zero", Count(TypeNamed(typeName), PartTime, Wednesday, Wednesday).Error?.Code);
    }

    // Full time and public holidays.

    [Theory]
    [InlineData("PaidLeave")]
    [InlineData("Rtt")]
    [InlineData("SickLeave")]
    [InlineData("UnpaidLeave")]
    public void Full_time_monday_to_friday_is_5_days_whatever_the_type(string typeName)
    {
        Assert.Equal(5m, Total(TypeNamed(typeName), FullTime, Monday, Friday));
    }

    [Fact]
    public void Half_days_at_both_ends_count_half_a_day_each()
    {
        var result = Count(new LeaveTypePaidLeave(), FullTime, Friday, NextMonday, HalfDay.Afternoon, HalfDay.Morning);

        Assert.Equal(1m, result.Value.Total);
    }

    [Fact]
    public void A_single_day_from_morning_to_afternoon_is_1_day()
    {
        Assert.Equal(1m, Total(new LeaveTypePaidLeave(), FullTime, Monday, Monday));
    }

    [Fact]
    public void A_weekend_alone_is_invalid()
    {
        Assert.Equal("duration.zero", Count(new LeaveTypePaidLeave(), FullTime, Saturday, Sunday).Error?.Code);
    }

    [Fact]
    public void A_public_holiday_alone_is_invalid()
    {
        var calendar = FakeLeaveCalendar.FullTime(Friday);

        Assert.Equal("duration.zero", Count(new LeaveTypePaidLeave(), calendar, Friday, Friday).Error?.Code);
    }

    // Day-by-day detail.

    [Fact]
    public void The_detail_explains_the_non_working_day_after_the_period()
    {
        var result = Count(new LeaveTypePaidLeave(), PartTime, Tuesday, Tuesday);

        Assert.Equal(
            [
                new CountedDay(Tuesday, DayKind.WorkingDay, 1m),
                new CountedDay(Wednesday, DayKind.NonWorkingDay, 1m),
            ],
            result.Value.Days);
    }

    [Fact]
    public void The_detail_lists_every_day_of_the_period_even_when_not_counted()
    {
        var result = Count(new LeaveTypePaidLeave(), FullTime, Friday, NextMonday);

        Assert.Equal(
            [
                new CountedDay(Friday, DayKind.WorkingDay, 1m),
                new CountedDay(Saturday, DayKind.Weekend, 0m),
                new CountedDay(Sunday, DayKind.Weekend, 0m),
                new CountedDay(NextMonday, DayKind.WorkingDay, 1m),
            ],
            result.Value.Days);
    }

    [Fact]
    public void The_detail_stops_at_the_end_of_the_period_when_nothing_more_counts()
    {
        var result = Count(new LeaveTypePaidLeave(), FullTime, Friday, Friday);

        Assert.Equal([new CountedDay(Friday, DayKind.WorkingDay, 1m)], result.Value.Days);
    }

    private static decimal Total(LeaveType type, ILeaveCalendar calendar, DateOnly first, DateOnly last) =>
        Count(type, calendar, first, last).Value.Total;

    private static Result<CountedDuration> Count(
        LeaveType type,
        ILeaveCalendar calendar,
        DateOnly first,
        DateOnly last,
        HalfDay start = HalfDay.Morning,
        HalfDay end = HalfDay.Afternoon)
    {
        var period = LeavePeriod.Create(new PeriodBound(first, start), new PeriodBound(last, end)).Value;

        return CountingPolicy.Count(period, type, calendar);
    }

    private static LeaveType TypeNamed(string name) => name switch
    {
        "PaidLeave" => new LeaveTypePaidLeave(),
        "Rtt" => new LeaveTypeRtt(),
        "SickLeave" => new LeaveTypeSickLeave(),
        "UnpaidLeave" => new LeaveTypeUnpaidLeave(),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };
}

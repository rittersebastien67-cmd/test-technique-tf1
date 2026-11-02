using HrAdministration.Application.Leaves;
using HrAdministration.Domain.HrCalendar;
using HrAdministration.Domain.Leaves;
using LeaveDayKind = HrAdministration.Domain.Leaves.DayKind;

namespace HrAdministration.Application.Tests.Leaves;

/// <summary>The Leaves counting policy on the real HrCalendar, through the adapter.</summary>
public sealed class HrCalendarLeaveCalendarTests
{
    private static readonly HrCalendarLeaveCalendar FullTime = new(new DayQualifier(PublicHolidayCalendar.General, WorkSchedule.FullTime5Days));
    private static readonly HrCalendarLeaveCalendar PartTime = new(new DayQualifier(PublicHolidayCalendar.General, WorkSchedule.PartTime80));
    private static readonly HrCalendarLeaveCalendar AlsaceMoselle = new(new DayQualifier(PublicHolidayCalendar.AlsaceMoselle, WorkSchedule.FullTime5Days));

    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly DateOnly Tuesday = new(2026, 11, 17);
    private static readonly DateOnly Wednesday = new(2026, 11, 18);
    private static readonly DateOnly Friday = new(2026, 11, 20);

    [Fact]
    public void Every_kind_of_day_is_translated()
    {
        Assert.Equal(LeaveDayKind.WorkingDay, PartTime.Qualify(Tuesday));
        Assert.Equal(LeaveDayKind.NonWorkingDay, PartTime.Qualify(Wednesday));
        Assert.Equal(LeaveDayKind.Weekend, PartTime.Qualify(new DateOnly(2026, 11, 21)));
        Assert.Equal(LeaveDayKind.PublicHoliday, PartTime.Qualify(new DateOnly(2026, 12, 25)));
    }

    // PRD table 4.1, on the real calendar.

    [Fact]
    public void Part_time_paid_leave_from_monday_to_friday_is_5_days()
    {
        Assert.Equal(5m, Count(PartTime, Monday, Friday, HalfDay.Morning, HalfDay.Afternoon).Value.Total);
    }

    [Fact]
    public void Part_time_paid_leave_on_tuesday_alone_is_2_days()
    {
        Assert.Equal(2m, Count(PartTime, Tuesday, Tuesday, HalfDay.Morning, HalfDay.Afternoon).Value.Total);
    }

    [Fact]
    public void Part_time_paid_leave_from_wednesday_to_friday_is_2_days()
    {
        Assert.Equal(2m, Count(PartTime, Wednesday, Friday, HalfDay.Morning, HalfDay.Afternoon).Value.Total);
    }

    [Fact]
    public void Part_time_paid_leave_on_wednesday_alone_is_invalid()
    {
        Assert.Equal("duration.zero", Count(PartTime, Wednesday, Wednesday, HalfDay.Morning, HalfDay.Afternoon).Error?.Code);
    }

    [Fact]
    public void Part_time_paid_leave_on_tuesday_afternoon_is_half_a_day()
    {
        Assert.Equal(0.5m, Count(PartTime, Tuesday, Tuesday, HalfDay.Afternoon, HalfDay.Afternoon).Value.Total);
    }

    // Public holidays of the employee's calendar (R11).

    [Fact]
    public void Wednesday_14_july_does_not_extend_a_part_time_tuesday()
    {
        var tuesday = new DateOnly(2027, 7, 13);

        Assert.Equal(1m, Count(PartTime, tuesday, tuesday, HalfDay.Morning, HalfDay.Afternoon).Value.Total);
    }

    [Fact]
    public void Good_friday_only_counts_outside_alsace_moselle()
    {
        var goodFriday = new DateOnly(2027, 3, 26);

        Assert.Equal(1m, Count(FullTime, goodFriday, goodFriday, HalfDay.Morning, HalfDay.Afternoon).Value.Total);
        Assert.Equal("duration.zero", Count(AlsaceMoselle, goodFriday, goodFriday, HalfDay.Morning, HalfDay.Afternoon).Error?.Code);
    }

    // The years the real calendar knows (2025 to 2030).

    [Theory]
    [InlineData(2024, 12, 31, false)]
    [InlineData(2025, 1, 1, true)]
    [InlineData(2030, 12, 31, true)]
    [InlineData(2031, 1, 1, false)]
    public void The_calendar_covers_the_years_it_knows(int year, int month, int day, bool covered)
    {
        Assert.Equal(covered, FullTime.Covers(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2024, 12, 30, 2024, 12, 31)]
    [InlineData(2024, 12, 30, 2025, 1, 3)]
    [InlineData(2030, 12, 30, 2031, 1, 3)]
    [InlineData(2031, 6, 2, 2031, 6, 6)]
    public void A_period_outside_the_known_years_is_refused(int firstYear, int firstMonth, int firstDay, int lastYear, int lastMonth, int lastDay)
    {
        var first = new DateOnly(firstYear, firstMonth, firstDay);
        var last = new DateOnly(lastYear, lastMonth, lastDay);

        Assert.Equal("period.outside_calendar", Count(FullTime, first, last, HalfDay.Morning, HalfDay.Afternoon).Error?.Code);
    }

    [Fact]
    public void The_last_day_of_the_known_years_is_counted_whatever_the_schedule()
    {
        // Tuesday, followed by New Year's Day and a worked Thursday: nothing else would count in 2031.
        var lastDay = new DateOnly(2030, 12, 31);

        Assert.Equal(1m, Count(FullTime, lastDay, lastDay, HalfDay.Morning, HalfDay.Afternoon).Value.Total);
        Assert.Equal(1m, Count(PartTime, lastDay, lastDay, HalfDay.Morning, HalfDay.Afternoon).Value.Total);
    }

    private static SharedKernel.Result<CountedDuration> Count(ILeaveCalendar calendar, DateOnly first, DateOnly last, HalfDay start, HalfDay end)
    {
        var period = LeavePeriod.Create(new PeriodBound(first, start), new PeriodBound(last, end)).Value;

        return CountingPolicy.Count(period, new LeaveTypePaidLeave(), calendar);
    }
}

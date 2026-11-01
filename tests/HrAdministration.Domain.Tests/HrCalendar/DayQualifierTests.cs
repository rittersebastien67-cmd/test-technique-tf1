using HrAdministration.Domain.HrCalendar;

namespace HrAdministration.Domain.Tests.HrCalendar;

public sealed class DayQualifierTests
{
    private static readonly DayQualifier FullTime = new(PublicHolidayCalendar.General, WorkSchedule.FullTime5Days);
    private static readonly DayQualifier PartTime = new(PublicHolidayCalendar.General, WorkSchedule.PartTime80);
    private static readonly DayQualifier AlsaceMoselle = new(PublicHolidayCalendar.AlsaceMoselle, WorkSchedule.FullTime5Days);

    [Fact]
    public void Full_time_week_with_a_holiday_is_qualified_day_by_day()
    {
        // Monday 12 to Sunday 18 July 2027, with Wednesday 14 July.
        var days = FullTime.Qualify(new DateOnly(2027, 7, 12), new DateOnly(2027, 7, 18));

        Assert.Equal(
            [
                DayKind.WorkingDay,
                DayKind.WorkingDay,
                DayKind.PublicHoliday,
                DayKind.WorkingDay,
                DayKind.WorkingDay,
                DayKind.Weekend,
                DayKind.Weekend,
            ],
            days.Select(day => day.Kind));
        Assert.Equal(new DateOnly(2027, 7, 18), days[^1].Date);
    }

    [Fact]
    public void Part_time_wednesday_is_a_non_working_day()
    {
        // Monday 16 to Friday 20 November 2026.
        var days = PartTime.Qualify(new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 20));

        Assert.Equal(
            [DayKind.WorkingDay, DayKind.WorkingDay, DayKind.NonWorkingDay, DayKind.WorkingDay, DayKind.WorkingDay],
            days.Select(day => day.Kind));
    }

    [Fact]
    public void A_holiday_on_a_non_working_wednesday_is_a_public_holiday()
    {
        Assert.Equal(DayKind.PublicHoliday, PartTime.Qualify(new DateOnly(2027, 7, 14)));
    }

    [Fact]
    public void A_holiday_on_a_sunday_is_a_public_holiday()
    {
        Assert.Equal(DayKind.PublicHoliday, FullTime.Qualify(new DateOnly(2027, 8, 15)));
    }

    [Fact]
    public void Good_friday_is_only_a_holiday_in_alsace_moselle()
    {
        var goodFriday = new DateOnly(2027, 3, 26);

        Assert.Equal(DayKind.PublicHoliday, AlsaceMoselle.Qualify(goodFriday));
        Assert.Equal(DayKind.WorkingDay, FullTime.Qualify(goodFriday));
    }

    [Fact]
    public void Twenty_sixth_of_december_is_only_a_holiday_in_alsace_moselle()
    {
        // A Sunday in 2027.
        var stStephensDay = new DateOnly(2027, 12, 26);

        Assert.Equal(DayKind.PublicHoliday, AlsaceMoselle.Qualify(stStephensDay));
        Assert.Equal(DayKind.Weekend, FullTime.Qualify(stStephensDay));
    }

    /// <summary>PRD 3.1: JRTT = 365 − weekend days − 25 CP − holidays on a weekday − 218, i.e. 11 days in 2027 and 10 in Alsace-Moselle.</summary>
    [Theory]
    [InlineData("General", 11)]
    [InlineData("AlsaceMoselle", 10)]
    public void Working_days_of_2027_match_the_jrtt_of_the_demo_data(string calendar, int expectedJrtt)
    {
        var qualifier = calendar == "General" ? FullTime : AlsaceMoselle;

        var workingDays = qualifier
            .Qualify(new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31))
            .Count(day => day.Kind == DayKind.WorkingDay);

        Assert.Equal(expectedJrtt, workingDays - 25 - 218);
    }

    [Fact]
    public void A_period_ending_before_it_starts_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FullTime.Qualify(new DateOnly(2027, 1, 2), new DateOnly(2027, 1, 1)));
    }
}

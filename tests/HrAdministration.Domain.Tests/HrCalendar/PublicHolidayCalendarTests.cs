using HrAdministration.Domain.HrCalendar;

namespace HrAdministration.Domain.Tests.HrCalendar;

public sealed class PublicHolidayCalendarTests
{
    [Fact]
    public void General_calendar_has_the_11_legal_holidays_of_2026()
    {
        DateOnly[] expected =
        [
            new(2026, 1, 1),
            new(2026, 4, 6),
            new(2026, 5, 1),
            new(2026, 5, 8),
            new(2026, 5, 14),
            new(2026, 5, 25),
            new(2026, 7, 14),
            new(2026, 8, 15),
            new(2026, 11, 1),
            new(2026, 11, 11),
            new(2026, 12, 25),
        ];

        Assert.Equal(expected, PublicHolidayCalendar.General.HolidaysIn(2026));
    }

    [Fact]
    public void General_calendar_has_the_11_legal_holidays_of_2027()
    {
        DateOnly[] expected =
        [
            new(2027, 1, 1),
            new(2027, 3, 29),
            new(2027, 5, 1),
            new(2027, 5, 6),
            new(2027, 5, 8),
            new(2027, 5, 17),
            new(2027, 7, 14),
            new(2027, 8, 15),
            new(2027, 11, 1),
            new(2027, 11, 11),
            new(2027, 12, 25),
        ];

        Assert.Equal(expected, PublicHolidayCalendar.General.HolidaysIn(2027));
    }

    [Theory]
    [InlineData(2026, 4, 3)]
    [InlineData(2027, 3, 26)]
    public void Alsace_Moselle_calendar_adds_good_friday_and_26_december(int year, int goodFridayMonth, int goodFridayDay)
    {
        var holidays = PublicHolidayCalendar.AlsaceMoselle.HolidaysIn(year);

        Assert.Equal(13, holidays.Count);
        Assert.Equal(
            [new DateOnly(year, goodFridayMonth, goodFridayDay), new DateOnly(year, 12, 26)],
            holidays.Except(PublicHolidayCalendar.General.HolidaysIn(year)));
    }

    [Theory]
    [InlineData(2025, 4, 21)]
    [InlineData(2026, 4, 6)]
    [InlineData(2027, 3, 29)]
    [InlineData(2028, 4, 17)]
    [InlineData(2029, 4, 2)]
    [InlineData(2030, 4, 22)]
    public void Easter_monday_is_known_from_2025_to_2030(int year, int month, int day)
    {
        Assert.True(PublicHolidayCalendar.General.IsPublicHoliday(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2024)]
    [InlineData(2031)]
    public void Years_without_a_known_easter_are_rejected(int year)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PublicHolidayCalendar.General.HolidaysIn(year));
    }
}

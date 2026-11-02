using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

/// <summary>A calendar without the HrCalendar module: public holidays are the ones given, and part time means Wednesday off.</summary>
internal sealed class FakeLeaveCalendar : ILeaveCalendar
{
    private readonly bool _worksOnWednesday;
    private readonly DateOnly[] _publicHolidays;
    private readonly DateOnly _lastCoveredDay;

    private FakeLeaveCalendar(bool worksOnWednesday, DateOnly[] publicHolidays, DateOnly lastCoveredDay)
    {
        _worksOnWednesday = worksOnWednesday;
        _publicHolidays = publicHolidays;
        _lastCoveredDay = lastCoveredDay;
    }

    public static FakeLeaveCalendar FullTime(params DateOnly[] publicHolidays) => new(worksOnWednesday: true, publicHolidays, DateOnly.MaxValue);

    public static FakeLeaveCalendar PartTime(params DateOnly[] publicHolidays) => new(worksOnWednesday: false, publicHolidays, DateOnly.MaxValue);

    public static FakeLeaveCalendar FullTimeUntil(DateOnly lastCoveredDay) => new(worksOnWednesday: true, [], lastCoveredDay);

    public bool Covers(DateOnly date) => date <= _lastCoveredDay;

    public DayKind Qualify(DateOnly date)
    {
        // Like the real calendar, which only knows some years.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(date, _lastCoveredDay);

        if (_publicHolidays.Contains(date))
        {
            return DayKind.PublicHoliday;
        }

        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return DayKind.Weekend;
        }

        return date.DayOfWeek == DayOfWeek.Wednesday && !_worksOnWednesday ? DayKind.NonWorkingDay : DayKind.WorkingDay;
    }
}

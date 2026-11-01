namespace HrAdministration.Domain.HrCalendar;

public readonly record struct QualifiedDay(DateOnly Date, DayKind Kind);

/// <summary>
/// Gives the <see cref="DayKind"/> of each day for an employee's calendar and schedule (R2, R11).
/// A public holiday wins over a weekend or a non-working day, so that it shows as such (e.g. Wednesday 14 July).
/// </summary>
public sealed class DayQualifier
{
    private readonly PublicHolidayCalendar _calendar;
    private readonly WorkSchedule _schedule;

    public DayQualifier(PublicHolidayCalendar calendar, WorkSchedule schedule)
    {
        _calendar = calendar;
        _schedule = schedule;
    }

    public DayKind Qualify(DateOnly date)
    {
        if (_calendar.IsPublicHoliday(date))
        {
            return DayKind.PublicHoliday;
        }

        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return DayKind.Weekend;
        }

        return _schedule.IsWorked(date.DayOfWeek) ? DayKind.WorkingDay : DayKind.NonWorkingDay;
    }

    /// <summary>Every day from <paramref name="first"/> to <paramref name="last"/>, both included.</summary>
    public IReadOnlyList<QualifiedDay> Qualify(DateOnly first, DateOnly last)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(last, first);

        var days = new List<QualifiedDay>(last.DayNumber - first.DayNumber + 1);
        for (var date = first; date <= last; date = date.AddDays(1))
        {
            days.Add(new QualifiedDay(date, Qualify(date)));
        }

        return days;
    }
}

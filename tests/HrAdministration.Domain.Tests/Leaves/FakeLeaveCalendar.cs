using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

/// <summary>A calendar without the HrCalendar module: public holidays are the ones given, and part time means Wednesday off.</summary>
internal sealed class FakeLeaveCalendar : ILeaveCalendar
{
    private readonly bool _worksOnWednesday;
    private readonly DateOnly[] _publicHolidays;

    private FakeLeaveCalendar(bool worksOnWednesday, DateOnly[] publicHolidays)
    {
        _worksOnWednesday = worksOnWednesday;
        _publicHolidays = publicHolidays;
    }

    public static FakeLeaveCalendar FullTime(params DateOnly[] publicHolidays) => new(worksOnWednesday: true, publicHolidays);

    public static FakeLeaveCalendar PartTime(params DateOnly[] publicHolidays) => new(worksOnWednesday: false, publicHolidays);

    public DayKind Qualify(DateOnly date)
    {
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

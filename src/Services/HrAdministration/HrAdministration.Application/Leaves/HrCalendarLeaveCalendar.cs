using HrAdministration.Domain.HrCalendar;
using HrAdministration.Domain.Leaves;
using CalendarDayKind = HrAdministration.Domain.HrCalendar.DayKind;
using LeaveDayKind = HrAdministration.Domain.Leaves.DayKind;

namespace HrAdministration.Application.Leaves;

/// <summary>Gives the Leaves module the days of HrCalendar. Modules communicate through this layer (section 1 of the architecture).</summary>
public sealed class HrCalendarLeaveCalendar : ILeaveCalendar
{
    private readonly DayQualifier _qualifier;

    public HrCalendarLeaveCalendar(DayQualifier qualifier)
    {
        _qualifier = qualifier;
    }

    public LeaveDayKind Qualify(DateOnly date) => _qualifier.Qualify(date) switch
    {
        CalendarDayKind.WorkingDay => LeaveDayKind.WorkingDay,
        CalendarDayKind.PublicHoliday => LeaveDayKind.PublicHoliday,
        CalendarDayKind.Weekend => LeaveDayKind.Weekend,
        CalendarDayKind.NonWorkingDay => LeaveDayKind.NonWorkingDay,
        var unknown => throw new ArgumentOutOfRangeException(nameof(date), unknown, "Unknown day kind."),
    };
}

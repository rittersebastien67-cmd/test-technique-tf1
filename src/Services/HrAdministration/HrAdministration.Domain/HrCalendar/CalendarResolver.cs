namespace HrAdministration.Domain.HrCalendar;

/// <summary>Chooses the public holiday calendar of an employee from their work location (R11, PRD D45).</summary>
public static class CalendarResolver
{
    private static readonly HashSet<string> AlsaceMoselleDepartments = ["57", "67", "68"];

    public static PublicHolidayCalendar Resolve(WorkLocation location) =>
        AlsaceMoselleDepartments.Contains(location.Department)
            ? PublicHolidayCalendar.AlsaceMoselle
            : PublicHolidayCalendar.General;
}

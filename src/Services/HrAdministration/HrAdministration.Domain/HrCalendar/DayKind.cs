namespace HrAdministration.Domain.HrCalendar;

/// <summary>What a calendar day is for a given employee. Only <see cref="WorkingDay"/> is a "jour ouvré" worked by the employee.</summary>
public enum DayKind
{
    /// <summary>Monday to Friday, not a public holiday, and worked according to the schedule.</summary>
    WorkingDay,

    /// <summary>A public holiday of the employee's calendar (R11), whatever the day of the week.</summary>
    PublicHoliday,

    /// <summary>Saturday or Sunday that is not a public holiday.</summary>
    Weekend,

    /// <summary>A weekday that the part-time schedule does not work, e.g. Wednesday for <see cref="WorkSchedule.PartTime80"/>.</summary>
    NonWorkingDay,
}

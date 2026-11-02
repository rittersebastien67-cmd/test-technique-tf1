namespace HrAdministration.Domain.Leaves;

/// <summary>What a calendar day is when counting a leave (R2).</summary>
public enum DayKind
{
    /// <summary>Monday to Friday, not a public holiday, and worked according to the schedule.</summary>
    WorkingDay,

    /// <summary>A public holiday of the employee's calendar (R11), whatever the day of the week.</summary>
    PublicHoliday,

    /// <summary>Saturday or Sunday that is not a public holiday.</summary>
    Weekend,

    /// <summary>A weekday that the part-time schedule does not work. Only paid leave can count it (PRD 4.1).</summary>
    NonWorkingDay,
}

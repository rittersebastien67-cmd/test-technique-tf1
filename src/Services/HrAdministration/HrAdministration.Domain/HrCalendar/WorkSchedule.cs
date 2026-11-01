namespace HrAdministration.Domain.HrCalendar;

/// <summary>Days of the week the employee works (PRD 3.1). Internal data of the HR record, never exposed.</summary>
public sealed class WorkSchedule
{
    public static readonly WorkSchedule FullTime5Days = new(
        nameof(FullTime5Days),
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]);

    /// <summary>80 % part time over 4 days: Wednesday is not worked (PRD D37).</summary>
    public static readonly WorkSchedule PartTime80 = new(
        nameof(PartTime80),
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday]);

    private readonly HashSet<DayOfWeek> _workedDays;

    private WorkSchedule(string name, HashSet<DayOfWeek> workedDays)
    {
        Name = name;
        _workedDays = workedDays;
    }

    public string Name { get; }

    public bool IsWorked(DayOfWeek day) => _workedDays.Contains(day);

    public override string ToString() => Name;
}

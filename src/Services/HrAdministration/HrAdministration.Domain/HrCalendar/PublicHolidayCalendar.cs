namespace HrAdministration.Domain.HrCalendar;

/// <summary>
/// Legal public holidays that apply to an employee, chosen from their work location (R11).
/// Days off by local custom are not public holidays (PRD D48).
/// </summary>
public sealed class PublicHolidayCalendar
{
    /// <summary>The 11 public holidays of the Labour Code (art. L3133-1).</summary>
    public static readonly PublicHolidayCalendar General = new(nameof(General), includesLocalLaw: false);

    /// <summary>
    /// Bas-Rhin, Haut-Rhin and Moselle (art. L3134-13): <see cref="General"/> plus Good Friday and 26 December, i.e. 13 days.
    /// Good Friday applies to the three whole departments (PRD D45).
    /// </summary>
    public static readonly PublicHolidayCalendar AlsaceMoselle = new(nameof(AlsaceMoselle), includesLocalLaw: true);

    private readonly bool _includesLocalLaw;

    private PublicHolidayCalendar(string name, bool includesLocalLaw)
    {
        Name = name;
        _includesLocalLaw = includesLocalLaw;
    }

    public string Name { get; }

    /// <summary>Public holidays of a civil year, in chronological order, including those falling on a weekend.</summary>
    public IReadOnlyList<DateOnly> HolidaysIn(int year)
    {
        var easter = Easter.Sunday(year);

        List<DateOnly> holidays =
        [
            new(year, 1, 1),
            easter.AddDays(1),   // Easter Monday
            new(year, 5, 1),
            new(year, 5, 8),
            easter.AddDays(39),  // Ascension Day
            easter.AddDays(50),  // Whit Monday
            new(year, 7, 14),
            new(year, 8, 15),
            new(year, 11, 1),
            new(year, 11, 11),
            new(year, 12, 25),
        ];

        if (_includesLocalLaw)
        {
            holidays.Add(easter.AddDays(-2)); // Good Friday
            holidays.Add(new DateOnly(year, 12, 26));
        }

        holidays.Sort();
        return holidays;
    }

    public bool IsPublicHoliday(DateOnly date) => HolidaysIn(date.Year).Contains(date);

    /// <summary>Whether the public holidays of this date are known. Asking for those of another date is a bug in the caller.</summary>
    public bool Covers(DateOnly date) => Easter.IsKnown(date.Year);

    public override string ToString() => Name;
}

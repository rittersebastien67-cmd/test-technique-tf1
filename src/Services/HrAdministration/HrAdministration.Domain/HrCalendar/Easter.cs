namespace HrAdministration.Domain.HrCalendar;

/// <summary>
/// Easter Sunday, from which movable public holidays are derived.
/// Dates are listed rather than computed: the application only handles a few years around today.
/// </summary>
internal static class Easter
{
    private static readonly Dictionary<int, DateOnly> Sundays = new()
    {
        [2025] = new(2025, 4, 20),
        [2026] = new(2026, 4, 5),
        [2027] = new(2027, 3, 28),
        [2028] = new(2028, 4, 16),
        [2029] = new(2029, 4, 1),
        [2030] = new(2030, 4, 21),
    };

    public static DateOnly Sunday(int year) =>
        Sundays.TryGetValue(year, out var sunday)
            ? sunday
            : throw new ArgumentOutOfRangeException(nameof(year), year, "Easter is only known from 2025 to 2030.");
}

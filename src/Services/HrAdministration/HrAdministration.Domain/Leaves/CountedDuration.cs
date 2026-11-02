namespace HrAdministration.Domain.Leaves;

/// <summary>What one calendar day adds to a duration: 0, 0.5 or 1 day.</summary>
public readonly record struct CountedDay(DateOnly Date, DayKind Kind, decimal Days);

/// <summary>Number of days a leave takes (R2), with its day-by-day detail so that a surprising count can be explained (A8).</summary>
public sealed class CountedDuration
{
    internal CountedDuration(IReadOnlyList<CountedDay> days)
    {
        Days = days;
        Total = days.Sum(day => day.Days);
    }

    public decimal Total { get; }

    /// <summary>In chronological order. Can go past the end of the period: a part-time paid leave also counts the non-working day before the return to work.</summary>
    public IReadOnlyList<CountedDay> Days { get; }
}

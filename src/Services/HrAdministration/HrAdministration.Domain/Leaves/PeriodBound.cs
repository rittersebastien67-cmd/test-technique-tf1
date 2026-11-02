namespace HrAdministration.Domain.Leaves;

/// <summary>Start or end of a leave period: a date and a half-day. The bound itself is included in the period.</summary>
public readonly record struct PeriodBound(DateOnly Date, HalfDay HalfDay) : IComparable<PeriodBound>
{
    public int CompareTo(PeriodBound other)
    {
        var byDate = Date.CompareTo(other.Date);
        return byDate != 0 ? byDate : HalfDay.CompareTo(other.HalfDay);
    }
}

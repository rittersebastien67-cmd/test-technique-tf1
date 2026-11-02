using SharedKernel;

namespace HrAdministration.Domain.Leaves;

/// <summary>
/// Period of absence (R1): the start bound is before or equal to the end bound.
/// On a single day, Morning→Morning and Afternoon→Afternoon are half a day, Morning→Afternoon a full day, and Afternoon→Morning is invalid.
/// </summary>
public sealed record LeavePeriod
{
    private LeavePeriod(PeriodBound start, PeriodBound end)
    {
        Start = start;
        End = end;
    }

    public PeriodBound Start { get; }

    public PeriodBound End { get; }

    public static Result<LeavePeriod> Create(PeriodBound start, PeriodBound end) =>
        start.CompareTo(end) > 0
            ? LeaveErrors.PeriodInvalid
            : new LeavePeriod(start, end);
}

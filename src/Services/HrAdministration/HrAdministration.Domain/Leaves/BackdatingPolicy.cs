using SharedKernel;

namespace HrAdministration.Domain.Leaves;

/// <summary>
/// A leave cannot start in the past, except a sick leave (R5), which is accepted with a warning beyond 48 hours.
/// A period has no time of day, so the 48 hours are two calendar days after the first day (AD65).
/// </summary>
public static class BackdatingPolicy
{
    private const int DeclarationDelayInDays = 2;

    /// <summary>The result is a success holding <c>null</c> when there is nothing to report.</summary>
    public static Result<Warning?> Check(DateOnly start, LeaveType type, DateOnly today)
    {
        if (start >= today)
        {
            return Result.Success<Warning?>(null);
        }

        if (type is not LeaveTypeSickLeave)
        {
            return LeaveErrors.BackdatingForbidden;
        }

        var isLate = today.DayNumber - start.DayNumber > DeclarationDelayInDays;
        return Result.Success(isLate ? Warning.LateDeclaration : null);
    }
}

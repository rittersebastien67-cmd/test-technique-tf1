namespace HrAdministration.Domain.Leaves;

/// <summary>
/// Another request of the employee, as <see cref="OverlapPolicy"/> sees it. Bounds rather than a <see cref="LeavePeriod"/>,
/// as a draft can have an invalid period (A3).
/// </summary>
public sealed record ExistingLeave(LeaveRequestStatus Status, PeriodBound Start, PeriodBound End);

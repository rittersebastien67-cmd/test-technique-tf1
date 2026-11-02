namespace HrAdministration.Domain.Leaves;

/// <summary>
/// Another request of the employee, as <see cref="SickLeaveOverridePolicy"/> sees it. Bounds rather than a <see cref="LeavePeriod"/>,
/// as a draft can have an invalid period (A3). <paramref name="Days"/> is the duration stored on the request, ignored unless it is submitted.
/// </summary>
public sealed record OverridableLeave(
    Guid LeaveRequestId,
    LeaveType Type,
    LeaveRequestStatus Status,
    PeriodBound Start,
    PeriodBound End,
    decimal Days);

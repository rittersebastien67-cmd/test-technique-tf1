namespace HrAdministration.Domain.Leaves;

/// <summary>What a sick leave leaves of a request on one side of it. It always counts at least half a day.</summary>
public sealed record LeavePart(LeavePeriod Period, CountedDuration Duration);

/// <summary>
/// What a sick leave does to one overlapped request (R10). It is shown to the employee before the submission (A7), then applied by the
/// Application layer: the request is superseded and fully credited, and each remaining part is posted as a new request and deducted.
/// </summary>
/// <param name="LeaveRequestId">The overlapped request.</param>
/// <param name="Type">The type of the overlapped request.</param>
/// <param name="Removed">The half-days shared with the sick leave.</param>
/// <param name="DaysRemoved">
/// What the employee gets back in the end. Can differ from the length of <paramref name="Removed"/>: part-time paid leave also counts
/// non-working days (PRD 4.1), which come and go with the worked day before them, so a shared non-working day can remove nothing.
/// </param>
/// <param name="RemainingParts">In chronological order: none, one on either side of the sick leave, or one on each side.</param>
public sealed record SickLeaveOverride(
    Guid LeaveRequestId,
    LeaveType Type,
    LeavePeriod Removed,
    decimal DaysRemoved,
    IReadOnlyList<LeavePart> RemainingParts)
{
    /// <summary>Unpaid leave has no counter: its days are removed, but none is given back.</summary>
    public decimal DaysCredited => Type is LeaveTypeUnpaidLeave ? 0m : DaysRemoved;
}

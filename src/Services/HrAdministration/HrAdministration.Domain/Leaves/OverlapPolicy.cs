using SharedKernel;

namespace HrAdministration.Domain.Leaves;

/// <summary>
/// Two submitted requests of an employee cannot share a half-day (R4): the morning and the afternoon of a same day are compatible.
/// Only submitted requests count, so a draft being checked never conflicts with itself.
/// The exception of a sick leave overriding other leaves (R10) belongs to <c>SickLeaveOverridePolicy</c>.
/// </summary>
public static class OverlapPolicy
{
    /// <summary>When several requests overlap, the error names the earliest one, whatever the order of the list.</summary>
    public static Result Check(LeavePeriod period, IEnumerable<ExistingLeave> existingLeaves)
    {
        var overlapped = existingLeaves
            .Where(leave => leave.Status == LeaveRequestStatus.Submitted)
            .Where(leave => period.Overlaps(leave.Start, leave.End))
            .MinBy(leave => leave.Start);

        return overlapped is null
            ? Result.Success()
            : LeaveErrors.Overlap(overlapped.Start.Date, overlapped.End.Date);
    }
}

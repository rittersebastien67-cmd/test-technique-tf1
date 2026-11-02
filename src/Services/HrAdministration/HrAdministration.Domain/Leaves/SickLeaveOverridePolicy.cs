using SharedKernel;

namespace HrAdministration.Domain.Leaves;

/// <summary>
/// A submitted sick leave wins over the submitted paid, RTT and unpaid leaves it overlaps, even started ones (R10).
/// Only the shared half-days are removed. Two sick leaves stay incompatible. Nothing changes here, so the
/// result is also the preview shown before the submission (A7).
/// </summary>
public static class SickLeaveOverridePolicy
{
    /// <summary>The overrides are in chronological order, whatever the order of the list. A request that is not overlapped has none.</summary>
    public static Result<IReadOnlyList<SickLeaveOverride>> Plan(
        LeavePeriod sickLeave,
        IEnumerable<OverridableLeave> existingLeaves,
        ILeaveCalendar calendar)
    {
        var overlapped = existingLeaves
            .Where(leave => leave.Status == LeaveRequestStatus.Submitted)
            .Where(leave => sickLeave.Overlaps(leave.Start, leave.End))
            .OrderBy(leave => leave.Start)
            .ToList();

        var otherSickLeave = overlapped.Find(leave => leave.Type is LeaveTypeSickLeave);
        if (otherSickLeave is not null)
        {
            return LeaveErrors.Overlap(otherSickLeave.Start.Date, otherSickLeave.End.Date);
        }

        var overrides = new List<SickLeaveOverride>();
        foreach (var leave in overlapped)
        {
            var leaveOverride = Override(leave, sickLeave, calendar);
            if (leaveOverride.IsFailure)
            {
                return leaveOverride.Error;
            }

            overrides.Add(leaveOverride.Value);
        }

        return Result.Success<IReadOnlyList<SickLeaveOverride>>(overrides);
    }

    private static Result<SickLeaveOverride> Override(OverridableLeave leave, LeavePeriod sickLeave, ILeaveCalendar calendar)
    {
        var before = Part(leave.Start, sickLeave.Start.PreviousHalfDay(), leave.Type, calendar);
        if (before.IsFailure)
        {
            return before.Error;
        }

        var after = Part(sickLeave.End.NextHalfDay(), leave.End, leave.Type, calendar);
        if (after.IsFailure)
        {
            return after.Error;
        }

        var removed = LeavePeriod.Create(Latest(leave.Start, sickLeave.Start), Earliest(leave.End, sickLeave.End)).Value;
        LeavePart[] remainingParts = [.. new[] { before.Value, after.Value }.OfType<LeavePart>()];

        // Counted again rather than subtracted: the days of a part-time paid leave depend on the days around them.
        var daysRemoved = leave.Days - remainingParts.Sum(part => part.Duration.Total);

        return new SickLeaveOverride(leave.LeaveRequestId, leave.Type, removed, daysRemoved, remainingParts);
    }

    /// <summary>Missing when the sick leave reaches this side of the request, or leaves only days that do not count, like a weekend.</summary>
    private static Result<LeavePart?> Part(PeriodBound start, PeriodBound end, LeaveType type, ILeaveCalendar calendar)
    {
        var period = LeavePeriod.Create(start, end);
        if (period.IsFailure)
        {
            return Result.Success<LeavePart?>(null);
        }

        var counted = CountingPolicy.Count(period.Value, type, calendar);
        if (counted.IsFailure)
        {
            return counted.Error == LeaveErrors.DurationZero ? Result.Success<LeavePart?>(null) : counted.Error;
        }

        return new LeavePart(period.Value, counted.Value);
    }

    private static PeriodBound Latest(PeriodBound first, PeriodBound second) => first.CompareTo(second) >= 0 ? first : second;

    private static PeriodBound Earliest(PeriodBound first, PeriodBound second) => first.CompareTo(second) <= 0 ? first : second;
}

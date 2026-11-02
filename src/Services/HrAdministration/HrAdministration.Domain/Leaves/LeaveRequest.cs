using SharedKernel;

namespace HrAdministration.Domain.Leaves;

/// <summary>
/// Leave request of an employee, rebuilt from its stream of events (C17). It is never deleted: it is cancelled.
/// A draft can break R1, R2 or R5: it is saved with an inconsistency (A3), and the same rule refuses its submission.
/// The rules that need other requests or counters (R3, R4, R9, R10) are checked by the Application layer.
/// </summary>
public sealed class LeaveRequest : AggregateRoot<Guid>
{
    private LeaveRequest()
    {
    }

    public Guid EmployeeId { get; private set; }

    public Origin Origin { get; private set; }

    public LeaveRequestStatus Status { get; private set; }

    public LeaveType Type { get; private set; } = null!;

    /// <summary>Kept as bounds rather than a <see cref="LeavePeriod"/>, as a draft can be saved with an invalid period (A3).</summary>
    public PeriodBound Start { get; private set; }

    public PeriodBound End { get; private set; }

    public Comment? Comment { get; private set; }

    /// <summary>Missing when the draft has no valid period or nothing to count. A submitted request always has one.</summary>
    public CountedDuration? Duration { get; private set; }

    public IReadOnlyList<Flag> Flags { get; private set; } = [];

    public IReadOnlyList<Warning> Warnings { get; private set; } = [];

    /// <summary>Only a comment refuses the draft: a sick leave must never store one (R6), and a <see cref="Comment"/> cannot be too long.</summary>
    public static Result<LeaveRequest> Draft(
        Guid id,
        Guid employeeId,
        Origin origin,
        DraftContent content,
        DateOnly today,
        ILeaveCalendar calendar)
    {
        var comment = Comment.Create(content.Comment, content.Type);
        if (comment.IsFailure)
        {
            return comment.Error;
        }

        var assessment = Assessment.Of(content.Type, content.Start, content.End, today, calendar);

        var request = new LeaveRequest();
        request.Raise(new LeaveRequestDrafted(
            id,
            employeeId,
            origin,
            content.Type,
            content.Start,
            content.End,
            comment.Value,
            assessment.Duration,
            assessment.FlagsWith(content.Assumptions),
            assessment.Warnings));
        return request;
    }

    public static LeaveRequest FromHistory(IEnumerable<IDomainEvent> history)
    {
        var request = new LeaveRequest();
        request.LoadFromHistory(history);
        return request;
    }

    /// <summary>Replaces the whole content, so a comment or an assumption that is not given again disappears.</summary>
    public Result Update(DraftContent content, DateOnly today, ILeaveCalendar calendar)
    {
        if (Status != LeaveRequestStatus.Draft)
        {
            return LeaveErrors.InvalidStatus;
        }

        var comment = Comment.Create(content.Comment, content.Type);
        if (comment.IsFailure)
        {
            return comment.Error;
        }

        var assessment = Assessment.Of(content.Type, content.Start, content.End, today, calendar);

        Raise(new LeaveRequestUpdated(
            Id,
            content.Type,
            content.Start,
            content.End,
            comment.Value,
            assessment.Duration,
            assessment.FlagsWith(content.Assumptions),
            assessment.Warnings));
        return Result.Success();
    }

    /// <summary>
    /// Checks the rules again on the day of the submission, not on the day of the draft: a start date can have passed
    /// in the meantime, and a sick leave can have become late. Replaying a submission raises nothing, so it deducts nothing.
    /// </summary>
    public Result Submit(DateOnly today, ILeaveCalendar calendar)
    {
        if (Status != LeaveRequestStatus.Draft)
        {
            return LeaveErrors.InvalidStatus;
        }

        var assessment = Assessment.Of(Type, Start, End, today, calendar);
        if (assessment.Errors.Count > 0)
        {
            return assessment.Errors[0];
        }

        Raise(new LeaveRequestSubmitted(Id, assessment.Duration!, assessment.Warnings));
        return Result.Success();
    }

    /// <summary>
    /// A draft can always be cancelled, a submitted request only before its first day (R7). A leave starting today has
    /// started, like the days that <c>LeaveBalance</c> counts as taken. Crediting the balance is up to the Application layer.
    /// </summary>
    public Result Cancel(DateOnly today)
    {
        if (Status is not (LeaveRequestStatus.Draft or LeaveRequestStatus.Submitted))
        {
            return LeaveErrors.InvalidStatus;
        }

        if (Status == LeaveRequestStatus.Submitted && Start.Date <= today)
        {
            return LeaveErrors.LeaveStarted;
        }

        Raise(new LeaveRequestCancelled(Id));
        return Result.Success();
    }

    /// <summary>
    /// A sick leave wins over the request, even a started one (R10), once <see cref="SickLeaveOverridePolicy"/> has decided. Unlike a cancellation,
    /// it is not a gesture of the employee, so R7 does not apply. Crediting the balance is up to the Application layer.
    /// </summary>
    public Result Supersede()
    {
        if (Status != LeaveRequestStatus.Submitted)
        {
            return LeaveErrors.InvalidStatus;
        }

        Raise(new LeaveRequestSuperseded(Id));
        return Result.Success();
    }

    /// <summary>
    /// Posts what a sick leave left of this superseded request as a new submitted request. R5 is not checked: a part of a started leave
    /// starts in the past. The new request is a request like any other, so R4, R7 and R10 apply to it on its own period.
    /// </summary>
    public Result<LeaveRequest> CarryOver(Guid leaveRequestId, LeavePart part)
    {
        if (Status != LeaveRequestStatus.Superseded)
        {
            return LeaveErrors.InvalidStatus;
        }

        var request = new LeaveRequest();
        request.Raise(new LeaveRequestCarriedOver(
            leaveRequestId,
            Id,
            EmployeeId,
            Origin,
            Type,
            part.Period.Start,
            part.Period.End,
            Comment,
            part.Duration,
            Flags,
            Warnings));
        return request;
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case LeaveRequestDrafted drafted:
                Id = drafted.LeaveRequestId;
                EmployeeId = drafted.EmployeeId;
                Origin = drafted.Origin;
                Status = LeaveRequestStatus.Draft;
                Type = drafted.Type;
                Start = drafted.Start;
                End = drafted.End;
                Comment = drafted.Comment;
                Duration = drafted.Duration;
                Flags = drafted.Flags;
                Warnings = drafted.Warnings;
                break;

            case LeaveRequestUpdated updated:
                Type = updated.Type;
                Start = updated.Start;
                End = updated.End;
                Comment = updated.Comment;
                Duration = updated.Duration;
                Flags = updated.Flags;
                Warnings = updated.Warnings;
                break;

            case LeaveRequestSubmitted submitted:
                Status = LeaveRequestStatus.Submitted;
                Duration = submitted.Duration;
                Warnings = submitted.Warnings;
                break;

            case LeaveRequestCancelled:
                Status = LeaveRequestStatus.Cancelled;
                break;

            case LeaveRequestSuperseded:
                Status = LeaveRequestStatus.Superseded;
                break;

            case LeaveRequestCarriedOver carriedOver:
                Id = carriedOver.LeaveRequestId;
                EmployeeId = carriedOver.EmployeeId;
                Origin = carriedOver.Origin;
                Status = LeaveRequestStatus.Submitted;
                Type = carriedOver.Type;
                Start = carriedOver.Start;
                End = carriedOver.End;
                Comment = carriedOver.Comment;
                Duration = carriedOver.Duration;
                Flags = carriedOver.Flags;
                Warnings = carriedOver.Warnings;
                break;
        }
    }

    /// <summary>What R1, R2 and R5 say about a content on a given day. <see cref="Duration"/> is only missing when there are errors.</summary>
    private sealed record Assessment(CountedDuration? Duration, IReadOnlyList<Error> Errors, IReadOnlyList<Warning> Warnings)
    {
        public static Assessment Of(LeaveType type, PeriodBound start, PeriodBound end, DateOnly today, ILeaveCalendar calendar)
        {
            CountedDuration? duration = null;
            var errors = new List<Error>();
            var warnings = new List<Warning>();

            var period = LeavePeriod.Create(start, end);
            if (period.IsFailure)
            {
                errors.Add(period.Error);
            }
            else
            {
                var counted = CountingPolicy.Count(period.Value, type, calendar);
                if (counted.IsFailure)
                {
                    errors.Add(counted.Error);
                }
                else
                {
                    duration = counted.Value;
                }
            }

            var backdating = BackdatingPolicy.Check(start.Date, type, today);
            if (backdating.IsFailure)
            {
                errors.Add(backdating.Error);
            }
            else if (backdating.Value is { } warning)
            {
                warnings.Add(warning);
            }

            return new Assessment(duration, errors, warnings);
        }

        public IReadOnlyList<Flag> FlagsWith(IEnumerable<string> assumptions) =>
            [.. assumptions.Select(Flag.Uncertainty), .. Errors.Select(Flag.Inconsistency)];
    }
}

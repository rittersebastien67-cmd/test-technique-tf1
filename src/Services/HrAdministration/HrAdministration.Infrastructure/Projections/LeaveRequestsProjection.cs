using HrAdministration.Domain.Leaves;
using HrAdministration.Infrastructure.EventSourcing;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace HrAdministration.Infrastructure.Projections;

/// <summary>Keeps <c>leave_requests_view</c> up to date, with the same transitions as <see cref="LeaveRequest"/>.</summary>
public sealed class LeaveRequestsProjection : IInlineProjection
{
    public async Task ApplyAsync(HrAdministrationDbContext context, string streamId, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case LeaveRequestDrafted drafted:
                context.LeaveRequestsView.Add(NewRecord(
                    drafted.LeaveRequestId,
                    drafted.EmployeeId,
                    drafted.Origin,
                    LeaveRequestStatus.Draft,
                    drafted.Type,
                    drafted.Start,
                    drafted.End,
                    drafted.Comment,
                    drafted.Duration,
                    drafted.Flags,
                    drafted.Warnings));
                break;

            case LeaveRequestCarriedOver carriedOver:
                context.LeaveRequestsView.Add(NewRecord(
                    carriedOver.LeaveRequestId,
                    carriedOver.EmployeeId,
                    carriedOver.Origin,
                    LeaveRequestStatus.Submitted,
                    carriedOver.Type,
                    carriedOver.Start,
                    carriedOver.End,
                    carriedOver.Comment,
                    carriedOver.Duration,
                    carriedOver.Flags,
                    carriedOver.Warnings));
                break;

            case LeaveRequestUpdated updated:
                var draft = await FindAsync(context, updated.LeaveRequestId, cancellationToken);
                draft.Type = updated.Type.Name;
                draft.StartDate = updated.Start.Date;
                draft.StartHalfDay = updated.Start.HalfDay;
                draft.EndDate = updated.End.Date;
                draft.EndHalfDay = updated.End.HalfDay;
                draft.Comment = updated.Comment?.Text;
                draft.Duration = SerializeDuration(updated.Duration);
                draft.Flags = EventSerializer.SerializeValue(updated.Flags);
                draft.Warnings = EventSerializer.SerializeValue(updated.Warnings);
                break;

            case LeaveRequestSubmitted submitted:
                var request = await FindAsync(context, submitted.LeaveRequestId, cancellationToken);
                request.Status = LeaveRequestStatus.Submitted;
                request.Duration = SerializeDuration(submitted.Duration);
                request.Warnings = EventSerializer.SerializeValue(submitted.Warnings);
                break;

            case LeaveRequestCancelled cancelled:
                (await FindAsync(context, cancelled.LeaveRequestId, cancellationToken)).Status = LeaveRequestStatus.Cancelled;
                break;

            case LeaveRequestSuperseded superseded:
                (await FindAsync(context, superseded.LeaveRequestId, cancellationToken)).Status = LeaveRequestStatus.Superseded;
                break;
        }
    }

    public Task ClearAsync(HrAdministrationDbContext context, CancellationToken cancellationToken) =>
        context.LeaveRequestsView.ExecuteDeleteAsync(cancellationToken);

    private static LeaveRequestViewRecord NewRecord(
        Guid leaveRequestId,
        Guid employeeId,
        Origin origin,
        LeaveRequestStatus status,
        LeaveType type,
        PeriodBound start,
        PeriodBound end,
        Comment? comment,
        CountedDuration? duration,
        IReadOnlyList<Flag> flags,
        IReadOnlyList<Warning> warnings) => new()
        {
            LeaveRequestId = leaveRequestId,
            EmployeeId = employeeId,
            Origin = origin,
            Status = status,
            Type = type.Name,
            StartDate = start.Date,
            StartHalfDay = start.HalfDay,
            EndDate = end.Date,
            EndHalfDay = end.HalfDay,
            Comment = comment?.Text,
            Duration = SerializeDuration(duration),
            Flags = EventSerializer.SerializeValue(flags),
            Warnings = EventSerializer.SerializeValue(warnings),
        };

    private static string? SerializeDuration(CountedDuration? duration) =>
        duration is null ? null : EventSerializer.SerializeValue(duration);

    /// <summary>Also finds a row added earlier in the same append. A stream always starts with the event that adds its row, so a missing one is a bug.</summary>
    private static async Task<LeaveRequestViewRecord> FindAsync(HrAdministrationDbContext context, Guid leaveRequestId, CancellationToken cancellationToken) =>
        await context.LeaveRequestsView.FindAsync([leaveRequestId], cancellationToken)
        ?? throw new InvalidOperationException($"The leave request {leaveRequestId} has no row in the view.");
}

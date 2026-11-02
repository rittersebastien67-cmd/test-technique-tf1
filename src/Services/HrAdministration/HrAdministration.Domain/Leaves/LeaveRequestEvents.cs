using SharedKernel;

namespace HrAdministration.Domain.Leaves;

/// <summary>
/// Opens a request as a draft. The bounds can be invalid and <paramref name="Duration"/> missing: the draft then carries its flags (A3).
/// The duration, flags and warnings are stored because projections are rebuilt by replay, without a calendar.
/// </summary>
public sealed record LeaveRequestDrafted(
    Guid LeaveRequestId,
    Guid EmployeeId,
    Origin Origin,
    LeaveType Type,
    PeriodBound Start,
    PeriodBound End,
    Comment? Comment,
    CountedDuration? Duration,
    IReadOnlyList<Flag> Flags,
    IReadOnlyList<Warning> Warnings) : IDomainEvent;

/// <summary>Replaces the whole content of a draft.</summary>
public sealed record LeaveRequestUpdated(
    Guid LeaveRequestId,
    LeaveType Type,
    PeriodBound Start,
    PeriodBound End,
    Comment? Comment,
    CountedDuration? Duration,
    IReadOnlyList<Flag> Flags,
    IReadOnlyList<Warning> Warnings) : IDomainEvent;

/// <summary>Records the explicit gesture of the employee, with the duration and warnings as they stood on that day.</summary>
public sealed record LeaveRequestSubmitted(
    Guid LeaveRequestId,
    CountedDuration Duration,
    IReadOnlyList<Warning> Warnings) : IDomainEvent;

/// <summary>Withdraws a draft or a submitted request (R7). The request stays in the stream with its history (C17).</summary>
public sealed record LeaveRequestCancelled(Guid LeaveRequestId) : IDomainEvent;

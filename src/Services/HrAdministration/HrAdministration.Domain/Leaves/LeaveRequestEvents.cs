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

/// <summary>A sick leave overlaps the request (R10). It stays in the stream with its last bounds (C17), and what is left of it is carried over.</summary>
public sealed record LeaveRequestSuperseded(Guid LeaveRequestId) : IDomainEvent;

/// <summary>
/// Opens the stream of a new submitted request, for what a sick leave left of <paramref name="SourceRequestId"/> (R10).
/// It is not a submission: no draft comes before it, and it carries what the source request had.
/// </summary>
public sealed record LeaveRequestCarriedOver(
    Guid LeaveRequestId,
    Guid SourceRequestId,
    Guid EmployeeId,
    Origin Origin,
    LeaveType Type,
    PeriodBound Start,
    PeriodBound End,
    Comment? Comment,
    CountedDuration Duration,
    IReadOnlyList<Flag> Flags,
    IReadOnlyList<Warning> Warnings) : IDomainEvent;

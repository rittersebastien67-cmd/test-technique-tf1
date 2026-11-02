using HrAdministration.Domain.Leaves;

namespace HrAdministration.Application.Leaves;

/// <summary>Leave request as stored in <c>leave_requests_view</c>, whatever its status. Same content as <see cref="LeaveRequest"/>, without its history.</summary>
public sealed record LeaveRequestView(
    Guid LeaveRequestId,
    Guid EmployeeId,
    Origin Origin,
    LeaveRequestStatus Status,
    LeaveType Type,
    PeriodBound Start,
    PeriodBound End,
    Comment? Comment,
    CountedDuration? Duration,
    IReadOnlyList<Flag> Flags,
    IReadOnlyList<Warning> Warnings);

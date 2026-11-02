using HrAdministration.Domain.Leaves;

namespace HrAdministration.Infrastructure.Projections;

/// <summary>A row of the <c>leave_requests_view</c> table. Bounds rather than a period, as a draft can have an invalid one (A3).</summary>
public sealed class LeaveRequestViewRecord
{
    public required Guid LeaveRequestId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required Origin Origin { get; init; }

    public required LeaveRequestStatus Status { get; set; }

    /// <summary>Name of a <see cref="LeaveType"/>.</summary>
    public required string Type { get; set; }

    public required DateOnly StartDate { get; set; }

    public required HalfDay StartHalfDay { get; set; }

    public required DateOnly EndDate { get; set; }

    public required HalfDay EndHalfDay { get; set; }

    public required string? Comment { get; set; }

    /// <summary>JSON of the days counted, in the format of the events. Missing when the draft has nothing to count.</summary>
    public required string? Duration { get; set; }

    /// <summary>JSON, in the format of the events.</summary>
    public required string Flags { get; set; }

    /// <summary>JSON, in the format of the events.</summary>
    public required string Warnings { get; set; }
}

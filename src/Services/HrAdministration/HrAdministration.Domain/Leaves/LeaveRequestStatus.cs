namespace HrAdministration.Domain.Leaves;

/// <summary>Life cycle of a leave request (PRD 5). A request is never deleted, whatever its status (C17).</summary>
public enum LeaveRequestStatus
{
    Draft,
    Submitted,
    Cancelled,

    /// <summary>Fully overridden by a sick leave (R10).</summary>
    Superseded,
}

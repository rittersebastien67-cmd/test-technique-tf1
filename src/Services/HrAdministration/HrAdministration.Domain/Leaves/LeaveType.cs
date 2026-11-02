namespace HrAdministration.Domain.Leaves;

/// <summary>
/// The four leave types of the V1 (PRD D51), as a closed hierarchy of value objects: two instances of a subclass are equal.
/// Business rules stay in the policies (AD28), not in the types.
/// </summary>
public abstract record LeaveType
{
    private protected LeaveType(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public virtual bool IsSensitive => false;

    public sealed override string ToString() => Name;
}

public sealed record LeaveTypePaidLeave() : LeaveType("PaidLeave");

public sealed record LeaveTypeRtt() : LeaveType("Rtt");

public sealed record LeaveTypeSickLeave() : LeaveType("SickLeave")
{
    /// <summary>Sensitive type: no comment (R6) and a generic category in the logs (C15).</summary>
    public override bool IsSensitive => true;
}

public sealed record LeaveTypeUnpaidLeave() : LeaveType("UnpaidLeave");

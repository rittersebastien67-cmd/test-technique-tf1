namespace HrAdministration.Domain.Leaves;

/// <summary>The four leave types of the V1 (PRD D51).</summary>
public sealed class LeaveType
{
    public static readonly LeaveType PaidLeave = new(nameof(PaidLeave), isSensitive: false);

    public static readonly LeaveType Rtt = new(nameof(Rtt), isSensitive: false);

    /// <summary>Sensitive type: no comment (R6) and a generic category in the logs (C15).</summary>
    public static readonly LeaveType SickLeave = new(nameof(SickLeave), isSensitive: true);

    public static readonly LeaveType UnpaidLeave = new(nameof(UnpaidLeave), isSensitive: false);

    private LeaveType(string name, bool isSensitive)
    {
        Name = name;
        IsSensitive = isSensitive;
    }

    public string Name { get; }

    public bool IsSensitive { get; }

    public override string ToString() => Name;
}

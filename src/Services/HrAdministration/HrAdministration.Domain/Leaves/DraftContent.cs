namespace HrAdministration.Domain.Leaves;

/// <summary>
/// What the employee or the agent fills in. The type and the bounds are required: nothing is saved before they are known (AD66).
/// <paramref name="Assumptions"/> are the codes of the hypotheses made by the agent.
/// </summary>
public sealed record DraftContent(
    LeaveType Type,
    PeriodBound Start,
    PeriodBound End,
    string? Comment,
    IReadOnlyList<string> Assumptions)
{
    /// <summary>Free text must never reach the logs (C3), even through an interpolated string.</summary>
    public override string ToString() => nameof(DraftContent);
}

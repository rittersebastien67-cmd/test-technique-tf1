using SharedKernel;

namespace HrAdministration.Domain.Leaves;

public enum FlagKind
{
    /// <summary>An assumption made by the agent, to be checked by the employee (A5).</summary>
    Uncertainty,

    /// <summary>A broken rule: the draft is saved, but its submission will be refused (A3).</summary>
    Inconsistency,
}

/// <summary>Remark carried by a draft. An uncertainty only has a code (such as <c>YEAR_INFERRED</c>), an inconsistency also explains the rule.</summary>
public sealed record Flag(FlagKind Kind, string Code, string? Message)
{
    public static Flag Uncertainty(string code) => new(FlagKind.Uncertainty, code, null);

    public static Flag Inconsistency(Error error) => new(FlagKind.Inconsistency, error.Code, error.Message);
}

namespace HrAdministration.Domain.Leaves;

/// <summary>Remark that never prevents a submission: it is returned with a successful response (AD29).</summary>
public sealed record Warning(string Code, string Message)
{
    public static readonly Warning LateDeclaration = new(
        "declaration.late",
        "Cet arrêt maladie est déclaré plus de 48 h après son début.");
}

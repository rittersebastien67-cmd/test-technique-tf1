namespace HrAdministration.Domain.Leaves;

/// <summary>Who prepared the request (C10). It never changes, even when the employee then edits a draft of the agent.</summary>
public enum Origin
{
    Manual,
    Agent,
}

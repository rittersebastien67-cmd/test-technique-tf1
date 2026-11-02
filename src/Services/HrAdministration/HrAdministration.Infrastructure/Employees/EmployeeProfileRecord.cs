namespace HrAdministration.Infrastructure.Employees;

/// <summary>A row of the <c>employee_profiles</c> table. The HR record is never modified in the V1 (AD55).</summary>
public sealed class EmployeeProfileRecord
{
    public required Guid EmployeeId { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string JobTitle { get; init; }

    public required string ManagerFirstName { get; init; }

    public required string ManagerLastName { get; init; }

    /// <summary>Name of a <c>WorkSchedule</c>.</summary>
    public required string WorkSchedule { get; init; }

    /// <summary>Department code of a <c>WorkLocation</c>.</summary>
    public required string WorkLocation { get; init; }
}

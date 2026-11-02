using HrAdministration.Domain.HrCalendar;

namespace HrAdministration.Application.Employees;

/// <summary>
/// HR record of an employee: a read model without any aggregate (AD55). It lives here because it reads the HrCalendar types, which no other Domain module
/// may know (AD59). Only the identity is exposed: <paramref name="WorkSchedule"/> (R2) and <paramref name="WorkLocation"/> (R11) stay internal.
/// </summary>
public sealed record EmployeeProfile(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    string JobTitle,
    DirectManager DirectManager,
    WorkSchedule WorkSchedule,
    WorkLocation WorkLocation)
{
    /// <summary>The identity must never reach the logs (C3), even through an interpolated string.</summary>
    public override string ToString() => nameof(EmployeeProfile);
}

/// <summary>Information shown on the HR record, without any role in the V1.</summary>
public sealed record DirectManager(string FirstName, string LastName)
{
    /// <summary>The identity must never reach the logs (C3), even through an interpolated string.</summary>
    public override string ToString() => nameof(DirectManager);
}

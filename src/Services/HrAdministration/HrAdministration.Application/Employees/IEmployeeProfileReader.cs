namespace HrAdministration.Application.Employees;

public interface IEmployeeProfileReader
{
    /// <summary>The HR record of an employee, or <c>null</c> if there is none.</summary>
    Task<EmployeeProfile?> FindAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

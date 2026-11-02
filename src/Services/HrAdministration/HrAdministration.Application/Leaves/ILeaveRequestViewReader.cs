namespace HrAdministration.Application.Leaves;

public interface ILeaveRequestViewReader
{
    /// <summary>The requests of an employee, whatever their status, by first day.</summary>
    Task<IReadOnlyList<LeaveRequestView>> ListAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

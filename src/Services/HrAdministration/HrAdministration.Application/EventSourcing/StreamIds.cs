using HrAdministration.Domain.Balances;

namespace HrAdministration.Application.EventSourcing;

/// <summary>Names of the streams (section 2.1 of the architecture).</summary>
public static class StreamIds
{
    public static string LeaveRequest(Guid leaveRequestId) => $"leave-request-{leaveRequestId}";

    public static string Balance(LeaveBalanceId id) => $"balance-{id.EmployeeId}-{id.Type}-{id.Year}";

    /// <summary>Concurrency guard of R4 and R10: it serializes the submissions of an employee (AD60).</summary>
    public static string Agenda(Guid employeeId) => $"agenda-{employeeId}";
}

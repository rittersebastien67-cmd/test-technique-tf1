namespace HrAdministration.Domain.Balances;

/// <summary>Identifies a counter: one per employee, type and year, like its <c>balance-{employee}-{type}-{year}</c> stream.</summary>
public sealed record LeaveBalanceId(Guid EmployeeId, BalanceType Type, int Year);

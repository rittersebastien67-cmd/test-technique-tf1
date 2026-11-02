using SharedKernel;

namespace HrAdministration.Domain.Balances;

/// <summary>Opens a counter with its entitlement (R8). <paramref name="CurrentYearOnly"/> marks the JRTT of the fixed-days profile (R9).</summary>
public sealed record EntitlementGranted(LeaveBalanceId BalanceId, decimal Days, bool CurrentYearOnly) : IDomainEvent;

/// <summary>Reserves days for a submitted leave request. <paramref name="FirstDay"/> tells taken days from planned ones.</summary>
public sealed record DaysDeducted(Guid LeaveRequestId, decimal Days, DateOnly FirstDay) : IDomainEvent;

/// <summary>Gives back days of a leave request, after a cancellation (R7) or a sick leave override (R10).</summary>
public sealed record DaysCredited(Guid LeaveRequestId, decimal Days) : IDomainEvent;

namespace HrAdministration.Domain.Balances;

/// <summary>Leave types limited by a counter. JRTT are an <see cref="Rtt"/> counter limited to the current year (AD63).</summary>
public enum BalanceType
{
    PaidLeave,
    Rtt,
}

namespace HrAdministration.Application.Balances;

public interface IBalanceViewReader
{
    /// <summary>The counters of an employee, by year then type.</summary>
    Task<IReadOnlyList<BalanceView>> ListAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

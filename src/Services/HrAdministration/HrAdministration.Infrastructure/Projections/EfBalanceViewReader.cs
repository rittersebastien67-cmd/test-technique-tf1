using HrAdministration.Application.Balances;
using HrAdministration.Domain.Balances;
using HrAdministration.Infrastructure.EventSourcing;
using Microsoft.EntityFrameworkCore;

namespace HrAdministration.Infrastructure.Projections;

public sealed class EfBalanceViewReader : IBalanceViewReader
{
    private readonly HrAdministrationDbContext _context;

    public EfBalanceViewReader(HrAdministrationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BalanceView>> ListAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var records = await _context.BalancesView
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId)
            .OrderBy(record => record.Year)
            .ThenBy(record => record.Type)
            .ToListAsync(cancellationToken);

        return [.. records.Select(record => new BalanceView(
            new LeaveBalanceId(record.EmployeeId, record.Type, record.Year),
            record.Acquired,
            record.CurrentYearOnly,
            EventSerializer.DeserializeValue<IReadOnlyList<BalanceReservation>>(record.Reservations)))];
    }
}

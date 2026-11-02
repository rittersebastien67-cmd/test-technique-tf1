using HrAdministration.Application.Leaves;
using HrAdministration.Domain.Leaves;
using HrAdministration.Infrastructure.EventSourcing;
using Microsoft.EntityFrameworkCore;

namespace HrAdministration.Infrastructure.Projections;

public sealed class EfLeaveRequestViewReader : ILeaveRequestViewReader
{
    private readonly HrAdministrationDbContext _context;

    public EfLeaveRequestViewReader(HrAdministrationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LeaveRequestView>> ListAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var records = await _context.LeaveRequestsView
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId)
            .OrderBy(record => record.StartDate)
            .ThenBy(record => record.LeaveRequestId)
            .ToListAsync(cancellationToken);

        return [.. records.Select(ToView)];
    }

    private static LeaveRequestView ToView(LeaveRequestViewRecord record) => new(
        record.LeaveRequestId,
        record.EmployeeId,
        record.Origin,
        record.Status,
        LeaveTypeJsonConverter.Parse(record.Type),
        new PeriodBound(record.StartDate, record.StartHalfDay),
        new PeriodBound(record.EndDate, record.EndHalfDay),
        record.Comment is null ? null : new Comment(record.Comment),
        record.Duration is null ? null : EventSerializer.DeserializeValue<CountedDuration>(record.Duration),
        EventSerializer.DeserializeValue<IReadOnlyList<Flag>>(record.Flags),
        EventSerializer.DeserializeValue<IReadOnlyList<Warning>>(record.Warnings));
}

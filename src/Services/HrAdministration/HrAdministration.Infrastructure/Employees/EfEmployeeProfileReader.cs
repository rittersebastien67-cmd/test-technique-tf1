using HrAdministration.Application.Employees;
using HrAdministration.Domain.HrCalendar;
using HrAdministration.Infrastructure.EventSourcing;
using Microsoft.EntityFrameworkCore;

namespace HrAdministration.Infrastructure.Employees;

public sealed class EfEmployeeProfileReader : IEmployeeProfileReader
{
    private readonly HrAdministrationDbContext _context;

    public EfEmployeeProfileReader(HrAdministrationDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeProfile?> FindAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var record = await _context.EmployeeProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.EmployeeId == employeeId, cancellationToken);

        return record is null
            ? null
            : new EmployeeProfile(
                record.EmployeeId,
                record.FirstName,
                record.LastName,
                record.JobTitle,
                new DirectManager(record.ManagerFirstName, record.ManagerLastName),
                ParseWorkSchedule(record.WorkSchedule),
                new WorkLocation(record.WorkLocation));
    }

    private static WorkSchedule ParseWorkSchedule(string name) => name switch
    {
        nameof(WorkSchedule.FullTime5Days) => WorkSchedule.FullTime5Days,
        nameof(WorkSchedule.PartTime80) => WorkSchedule.PartTime80,
        _ => throw new InvalidOperationException($"The stored work schedule {name} is unknown."),
    };
}

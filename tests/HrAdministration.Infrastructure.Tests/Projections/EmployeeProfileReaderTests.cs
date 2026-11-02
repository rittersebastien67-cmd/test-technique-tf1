using HrAdministration.Application.Employees;
using HrAdministration.Domain.HrCalendar;
using HrAdministration.Infrastructure.Employees;
using HrAdministration.Infrastructure.Tests.EventSourcing;

namespace HrAdministration.Infrastructure.Tests.Projections;

public sealed class EmployeeProfileReaderTests : IDisposable
{
    private readonly SqliteEventStoreDatabase _database = new();

    public void Dispose() => _database.Dispose();

    internal static EmployeeProfileRecord Profile(Guid employeeId, string workSchedule = nameof(WorkSchedule.PartTime80)) => new()
    {
        EmployeeId = employeeId,
        FirstName = "Camille",
        LastName = "Martin",
        JobTitle = "Développeur",
        ManagerFirstName = "Alex",
        ManagerLastName = "Durand",
        WorkSchedule = workSchedule,
        WorkLocation = "57",
    };

    [Fact]
    public async Task A_profile_is_read_with_its_identity_and_its_internal_data()
    {
        var employeeId = Guid.NewGuid();
        await Store(Profile(employeeId));

        var profile = await Reader().FindAsync(employeeId);

        Assert.NotNull(profile);
        Assert.Equal(employeeId, profile.EmployeeId);
        Assert.Equal("Camille", profile.FirstName);
        Assert.Equal("Martin", profile.LastName);
        Assert.Equal("Développeur", profile.JobTitle);
        Assert.Equal(new DirectManager("Alex", "Durand"), profile.DirectManager);
        Assert.Same(WorkSchedule.PartTime80, profile.WorkSchedule);
        Assert.Equal(new WorkLocation("57"), profile.WorkLocation);
    }

    [Theory]
    [InlineData(nameof(WorkSchedule.FullTime5Days))]
    [InlineData(nameof(WorkSchedule.PartTime80))]
    public async Task Every_work_schedule_can_be_read(string name)
    {
        var employeeId = Guid.NewGuid();
        await Store(Profile(employeeId, name));

        Assert.Equal(name, (await Reader().FindAsync(employeeId))?.WorkSchedule.Name);
    }

    [Fact]
    public async Task An_unknown_work_schedule_is_a_bug_in_the_stored_data()
    {
        var employeeId = Guid.NewGuid();
        await Store(Profile(employeeId, "Unknown"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Reader().FindAsync(employeeId));
    }

    [Fact]
    public async Task An_employee_without_a_record_has_no_profile()
    {
        await Store(Profile(Guid.NewGuid()));

        Assert.Null(await Reader().FindAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task The_identity_cannot_reach_the_logs_through_an_interpolated_string()
    {
        var employeeId = Guid.NewGuid();
        await Store(Profile(employeeId));

        var profile = await Reader().FindAsync(employeeId);

        Assert.Equal(nameof(EmployeeProfile), profile!.ToString());
        Assert.Equal(nameof(DirectManager), profile.DirectManager.ToString());
    }

    private EfEmployeeProfileReader Reader() => new(_database.NewContext());

    private async Task Store(EmployeeProfileRecord record)
    {
        await using var context = _database.NewContext();
        context.EmployeeProfiles.Add(record);
        await context.SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HrAdministration.Infrastructure.EventSourcing;

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without starting the host. The tool does not connect to this address.</summary>
public sealed class HrAdministrationDbContextFactory : IDesignTimeDbContextFactory<HrAdministrationDbContext>
{
    public HrAdministrationDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<HrAdministrationDbContext>().UseNpgsql("Host=localhost").Options);
}

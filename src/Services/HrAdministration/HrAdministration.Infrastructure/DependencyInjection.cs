using HrAdministration.Application.EventSourcing;
using HrAdministration.Infrastructure.EventSourcing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HrAdministration.Infrastructure;

public static class DependencyInjection
{
    /// <summary>The connection string is only required once the database is used: without one, the host still starts and maps its routes.</summary>
    public static IServiceCollection AddHrAdministrationInfrastructure(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<HrAdministrationDbContext>(options => options.UseNpgsql(
            connectionString ?? throw new InvalidOperationException("The connection string of the HrAdministration database is missing.")));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IUniqueViolationDetector, PostgresUniqueViolationDetector>();
        services.AddScoped<IEventStore, EfEventStore>();

        return services;
    }

    public static async Task MigrateHrAdministrationDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HrAdministrationDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}

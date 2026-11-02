using HrAdministration.Application.Balances;
using HrAdministration.Application.Employees;
using HrAdministration.Application.EventSourcing;
using HrAdministration.Application.Leaves;
using HrAdministration.Infrastructure.Employees;
using HrAdministration.Infrastructure.EventSourcing;
using HrAdministration.Infrastructure.Projections;
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
        services.AddSingleton<IInlineProjection, LeaveRequestsProjection>();
        services.AddSingleton<IInlineProjection, BalancesProjection>();
        services.AddScoped<IEventStore, EfEventStore>();
        services.AddScoped<ProjectionRebuilder>();
        services.AddScoped<ILeaveRequestViewReader, EfLeaveRequestViewReader>();
        services.AddScoped<IBalanceViewReader, EfBalanceViewReader>();
        services.AddScoped<IEmployeeProfileReader, EfEmployeeProfileReader>();

        return services;
    }

    public static async Task MigrateHrAdministrationDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HrAdministrationDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }

    public static async Task RebuildHrAdministrationProjectionsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ProjectionRebuilder>().RebuildAsync(cancellationToken);
    }
}

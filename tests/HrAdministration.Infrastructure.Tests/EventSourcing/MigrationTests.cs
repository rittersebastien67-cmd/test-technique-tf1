using HrAdministration.Application.Balances;
using HrAdministration.Application.Employees;
using HrAdministration.Application.EventSourcing;
using HrAdministration.Application.Leaves;
using HrAdministration.Infrastructure.Employees;
using HrAdministration.Infrastructure.EventSourcing;
using HrAdministration.Infrastructure.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HrAdministration.Infrastructure.Tests.EventSourcing;

/// <summary>What only PostgreSQL does, checked without a server (AD56): the SQL of the migrations, the detection of a conflict and the wiring.</summary>
public sealed class MigrationTests
{
    [Fact]
    public void The_migrations_match_the_model()
    {
        using var context = new HrAdministrationDbContextFactory().CreateDbContext([]);

        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void The_table_has_the_expected_columns_and_a_unique_key_on_stream_and_version()
    {
        var script = MigrationScript();

        Assert.Contains("stream_id text NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("version integer NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("type text NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("data jsonb NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("metadata jsonb NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("recorded_at timestamp with time zone NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("CONSTRAINT pk_events PRIMARY KEY (stream_id, version)", script, StringComparison.Ordinal);
    }

    [Fact]
    public void The_database_refuses_any_update_delete_or_truncate()
    {
        var script = MigrationScript();

        Assert.Contains("BEFORE UPDATE OR DELETE ON events", script, StringComparison.Ordinal);
        Assert.Contains("BEFORE TRUNCATE ON events", script, StringComparison.Ordinal);
    }

    [Fact]
    public void The_read_models_have_their_own_tables()
    {
        var script = MigrationScript();

        Assert.Contains("CONSTRAINT pk_leave_requests_view PRIMARY KEY (leave_request_id)", script, StringComparison.Ordinal);
        Assert.Contains("CONSTRAINT pk_balances_view PRIMARY KEY (stream_id)", script, StringComparison.Ordinal);
        Assert.Contains("CONSTRAINT pk_employee_profiles PRIMARY KEY (employee_id)", script, StringComparison.Ordinal);
        Assert.Contains("reservations jsonb NOT NULL", script, StringComparison.Ordinal);
    }

    [Fact]
    public void A_postgres_unique_violation_is_a_conflict()
    {
        var exception = new DbUpdateException("Failed.", new PostgresException("Duplicate.", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation));

        Assert.True(new PostgresUniqueViolationDetector().IsUniqueViolation(exception));
    }

    [Fact]
    public void Any_other_failure_is_not_a_conflict()
    {
        var detector = new PostgresUniqueViolationDetector();

        Assert.False(detector.IsUniqueViolation(new DbUpdateException("Failed.", new PostgresException("Null.", "ERROR", "ERROR", PostgresErrorCodes.NotNullViolation))));
        Assert.False(detector.IsUniqueViolation(new DbUpdateException("Failed.")));
    }

    [Fact]
    public async Task The_event_store_is_wired_without_connecting()
    {
        await using var services = new ServiceCollection().AddHrAdministrationInfrastructure("Host=localhost").BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();

        Assert.IsType<EfEventStore>(scope.ServiceProvider.GetRequiredService<IEventStore>());
    }

    [Fact]
    public async Task The_read_models_are_wired_without_connecting()
    {
        await using var services = new ServiceCollection().AddHrAdministrationInfrastructure("Host=localhost").BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();

        Assert.Equal(
            [typeof(LeaveRequestsProjection), typeof(BalancesProjection)],
            scope.ServiceProvider.GetServices<IInlineProjection>().Select(projection => projection.GetType()));
        Assert.IsType<EfLeaveRequestViewReader>(scope.ServiceProvider.GetRequiredService<ILeaveRequestViewReader>());
        Assert.IsType<EfBalanceViewReader>(scope.ServiceProvider.GetRequiredService<IBalanceViewReader>());
        Assert.IsType<EfEmployeeProfileReader>(scope.ServiceProvider.GetRequiredService<IEmployeeProfileReader>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ProjectionRebuilder>());
    }

    [Fact]
    public async Task A_missing_connection_string_only_fails_when_the_database_is_used()
    {
        await using var services = new ServiceCollection().AddHrAdministrationInfrastructure(null).BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();

        Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IEventStore>());
    }

    private static string MigrationScript()
    {
        using var context = new HrAdministrationDbContextFactory().CreateDbContext([]);
        return context.GetService<IMigrator>().GenerateScript();
    }
}

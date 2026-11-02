using HrAdministration.Infrastructure.EventSourcing;
using HrAdministration.Infrastructure.Projections;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrAdministration.Infrastructure.Tests.EventSourcing;

/// <summary>
/// An in-memory database with the real <c>events</c> table, without Docker (AD56). Unlike a fake store, it enforces the key and rolls back
/// transactions. Each store has its own context, like two requests. The triggers are PostgreSQL ones: <see cref="MigrationTests"/> covers them.
/// <see cref="NewStore"/> has no projection, so that a test can store any event in any stream.
/// </summary>
internal sealed class SqliteEventStoreDatabase : IDisposable
{
    public static readonly DateTimeOffset Now = new(2026, 11, 2, 10, 30, 0, TimeSpan.Zero);

    private static readonly IInlineProjection[] Projections = [new LeaveRequestsProjection(), new BalancesProjection()];

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteEventStoreDatabase()
    {
        _connection.Open();

        using var context = NewContext();
        context.Database.EnsureCreated();
    }

    public HrAdministrationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<HrAdministrationDbContext>().UseSqlite(_connection).Options);

    public EfEventStore NewStore() => NewStore([]);

    public EfEventStore NewProjectingStore() => NewStore(Projections);

    public ProjectionRebuilder NewRebuilder() => new(NewContext(), Projections);

    public void Dispose() => _connection.Dispose();

    private EfEventStore NewStore(IInlineProjection[] projections) =>
        new(NewContext(), new FixedTimeProvider(), new SqliteUniqueViolationDetector(), projections);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class SqliteUniqueViolationDetector : IUniqueViolationDetector
    {
        private const int PrimaryKeyViolation = 1555;

        public bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is SqliteException { SqliteExtendedErrorCode: PrimaryKeyViolation };
    }
}

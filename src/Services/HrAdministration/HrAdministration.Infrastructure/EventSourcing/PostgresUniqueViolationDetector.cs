using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HrAdministration.Infrastructure.EventSourcing;

public sealed class PostgresUniqueViolationDetector : IUniqueViolationDetector
{
    public bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}

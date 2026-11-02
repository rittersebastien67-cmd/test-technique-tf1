using Microsoft.EntityFrameworkCore;

namespace HrAdministration.Infrastructure.EventSourcing;

/// <summary>Recognizes a unique violation, which each database provider reports differently. The tests run on another provider than production.</summary>
public interface IUniqueViolationDetector
{
    bool IsUniqueViolation(DbUpdateException exception);
}

using HrAdministration.Domain.Balances;

namespace HrAdministration.Infrastructure.Projections;

/// <summary>A row of the <c>balances_view</c> table. Keyed by stream, as only <see cref="EntitlementGranted"/> tells which counter a stream is.</summary>
public sealed class BalanceViewRecord
{
    public required string StreamId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required BalanceType Type { get; init; }

    public required int Year { get; init; }

    public required decimal Acquired { get; init; }

    public required bool CurrentYearOnly { get; init; }

    /// <summary>JSON of what each request holds: the first days are needed on read to split taken and planned days.</summary>
    public required string Reservations { get; set; }
}

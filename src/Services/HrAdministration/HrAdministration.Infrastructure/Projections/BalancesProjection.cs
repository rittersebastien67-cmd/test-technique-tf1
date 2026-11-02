using HrAdministration.Application.Balances;
using HrAdministration.Domain.Balances;
using HrAdministration.Infrastructure.EventSourcing;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace HrAdministration.Infrastructure.Projections;

/// <summary>Keeps <c>balances_view</c> up to date, with the same transitions as <see cref="LeaveBalance"/>.</summary>
public sealed class BalancesProjection : IInlineProjection
{
    public async Task ApplyAsync(HrAdministrationDbContext context, string streamId, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case EntitlementGranted granted:
                context.BalancesView.Add(new BalanceViewRecord
                {
                    StreamId = streamId,
                    EmployeeId = granted.BalanceId.EmployeeId,
                    Type = granted.BalanceId.Type,
                    Year = granted.BalanceId.Year,
                    Acquired = granted.Days,
                    CurrentYearOnly = granted.CurrentYearOnly,
                    Reservations = EventSerializer.SerializeValue<IReadOnlyList<BalanceReservation>>([]),
                });
                break;

            case DaysDeducted deducted:
                await ChangeReservationsAsync(context, streamId, cancellationToken, reservations =>
                    [.. reservations, new BalanceReservation(deducted.LeaveRequestId, deducted.FirstDay, deducted.Days)]);
                break;

            case DaysCredited credited:
                await ChangeReservationsAsync(context, streamId, cancellationToken, reservations =>
                    [.. reservations.Select(reservation => reservation.LeaveRequestId == credited.LeaveRequestId
                        ? reservation with { Days = reservation.Days - credited.Days }
                        : reservation)]);
                break;
        }
    }

    public Task ClearAsync(HrAdministrationDbContext context, CancellationToken cancellationToken) =>
        context.BalancesView.ExecuteDeleteAsync(cancellationToken);

    /// <summary>Also finds a row added earlier in the same append. A stream always starts with its entitlement, so a missing row is a bug.</summary>
    private static async Task ChangeReservationsAsync(
        HrAdministrationDbContext context,
        string streamId,
        CancellationToken cancellationToken,
        Func<IReadOnlyList<BalanceReservation>, IReadOnlyList<BalanceReservation>> change)
    {
        var record = await context.BalancesView.FindAsync([streamId], cancellationToken)
            ?? throw new InvalidOperationException($"The stream {streamId} has no row in the view.");

        record.Reservations = EventSerializer.SerializeValue(
            change(EventSerializer.DeserializeValue<IReadOnlyList<BalanceReservation>>(record.Reservations)));
    }
}

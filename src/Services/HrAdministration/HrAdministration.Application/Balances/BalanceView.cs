using HrAdministration.Domain.Balances;

namespace HrAdministration.Application.Balances;

/// <summary>
/// Counter as stored in <c>balances_view</c>. Like <see cref="LeaveBalance"/>, it keeps what each request reserved: the split between taken and planned
/// days depends on the date, so it is computed on read and never stored.
/// </summary>
public sealed record BalanceView(LeaveBalanceId Id, decimal Acquired, bool CurrentYearOnly, IReadOnlyList<BalanceReservation> Reservations)
{
    public decimal Reserved => Reservations.Sum(reservation => reservation.Days);

    public decimal Available => Acquired - Reserved;

    public decimal Taken(DateOnly today) =>
        Reservations.Where(reservation => reservation.FirstDay <= today).Sum(reservation => reservation.Days);

    public decimal Planned(DateOnly today) =>
        Reservations.Where(reservation => reservation.FirstDay > today).Sum(reservation => reservation.Days);
}

/// <summary>Days a leave request still holds on a counter. Kept at zero once fully credited, like in <see cref="LeaveBalance"/>.</summary>
public sealed record BalanceReservation(Guid LeaveRequestId, DateOnly FirstDay, decimal Days);

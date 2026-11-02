using SharedKernel;

namespace HrAdministration.Domain.Balances;

/// <summary>
/// Counter of an employee for one type and one year, rebuilt from its ledger of events (AD30).
/// Reserved days split into taken ones (period started or over) and planned ones (period to come).
/// The split depends on the date, so it is computed on read and never stored.
/// </summary>
public sealed class LeaveBalance : AggregateRoot<LeaveBalanceId>
{
    private readonly Dictionary<Guid, Reservation> _reservations = [];

    private LeaveBalance()
    {
    }

    public decimal Acquired { get; private set; }

    /// <summary>The counter only applies to the current calendar year (R9).</summary>
    public bool CurrentYearOnly { get; private set; }

    public decimal Reserved => _reservations.Values.Sum(reservation => reservation.Days);

    public decimal Available => Acquired - Reserved;

    public static LeaveBalance Grant(LeaveBalanceId id, decimal days, bool currentYearOnly)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(days);

        var balance = new LeaveBalance();
        balance.Raise(new EntitlementGranted(id, days, currentYearOnly));
        return balance;
    }

    public static LeaveBalance FromHistory(IEnumerable<IDomainEvent> history)
    {
        var balance = new LeaveBalance();
        balance.LoadFromHistory(history);
        return balance;
    }

    public decimal Taken(DateOnly today) =>
        _reservations.Values.Where(reservation => reservation.FirstDay <= today).Sum(reservation => reservation.Days);

    public decimal Planned(DateOnly today) =>
        _reservations.Values.Where(reservation => reservation.FirstDay > today).Sum(reservation => reservation.Days);

    /// <summary>
    /// Reserves the days of a leave request (R3, R9). A request is deducted only once: deducting it again succeeds
    /// without any new event, even if the rules would now refuse it, so replaying a submission stays harmless.
    /// A period lies within a single calendar year and belongs to the counter of that year.
    /// </summary>
    public Result Deduct(Guid leaveRequestId, decimal days, DateOnly firstDay, DateOnly lastDay, DateOnly today)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);

        if (_reservations.ContainsKey(leaveRequestId))
        {
            return Result.Success();
        }

        if (CurrentYearOnly && (today.Year != Id.Year || firstDay.Year != Id.Year || lastDay.Year != Id.Year))
        {
            return BalanceErrors.OutsideCurrentYear;
        }

        if (firstDay.Year != lastDay.Year)
        {
            return BalanceErrors.SpansTwoYears(Id.Type);
        }

        if (firstDay.Year != Id.Year)
        {
            return BalanceErrors.OutsideCounterYear(Id);
        }

        if (days > Available)
        {
            return BalanceErrors.Insufficient(Id.Type, days, Available);
        }

        Raise(new DaysDeducted(leaveRequestId, days, firstDay));
        return Result.Success();
    }

    /// <summary>Gives back days reserved by a leave request. Crediting more than it reserved is a bug in the caller, not a business rule.</summary>
    public void Credit(Guid leaveRequestId, decimal days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);

        if (!_reservations.TryGetValue(leaveRequestId, out var reservation) || days > reservation.Days)
        {
            throw new InvalidOperationException("Cannot credit more days than the leave request reserved.");
        }

        Raise(new DaysCredited(leaveRequestId, days));
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case EntitlementGranted granted:
                Id = granted.BalanceId;
                Acquired = granted.Days;
                CurrentYearOnly = granted.CurrentYearOnly;
                break;

            case DaysDeducted deducted:
                _reservations[deducted.LeaveRequestId] = new Reservation(deducted.FirstDay, deducted.Days);
                break;

            case DaysCredited credited:
                var reservation = _reservations[credited.LeaveRequestId];
                _reservations[credited.LeaveRequestId] = reservation with { Days = reservation.Days - credited.Days };
                break;
        }
    }

    /// <summary>Days a leave request still holds. Kept at zero once fully credited, so that the request cannot be deducted again.</summary>
    private readonly record struct Reservation(DateOnly FirstDay, decimal Days);
}

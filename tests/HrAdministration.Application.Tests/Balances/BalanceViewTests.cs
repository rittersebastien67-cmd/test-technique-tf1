using HrAdministration.Application.Balances;
using HrAdministration.Domain.Balances;

namespace HrAdministration.Application.Tests.Balances;

public sealed class BalanceViewTests
{
    private static readonly DateOnly Today = new(2026, 11, 2);

    private static BalanceView View(params BalanceReservation[] reservations) =>
        new(new LeaveBalanceId(Guid.NewGuid(), BalanceType.PaidLeave, 2026), 25m, false, reservations);

    [Fact]
    public void A_counter_without_reservation_is_entirely_available()
    {
        var view = View();

        Assert.Equal(0m, view.Reserved);
        Assert.Equal(25m, view.Available);
        Assert.Equal(0m, view.Taken(Today));
        Assert.Equal(0m, view.Planned(Today));
    }

    [Fact]
    public void A_leave_starting_today_is_taken_and_one_starting_tomorrow_is_planned()
    {
        var view = View(
            new BalanceReservation(Guid.NewGuid(), Today, 2m),
            new BalanceReservation(Guid.NewGuid(), Today.AddDays(1), 3.5m));

        Assert.Equal(2m, view.Taken(Today));
        Assert.Equal(3.5m, view.Planned(Today));
        Assert.Equal(5.5m, view.Reserved);
        Assert.Equal(19.5m, view.Available);
    }

    [Fact]
    public void A_fully_credited_reservation_counts_for_nothing()
    {
        var view = View(new BalanceReservation(Guid.NewGuid(), Today, 0m));

        Assert.Equal(25m, view.Available);
        Assert.Equal(0m, view.Taken(Today));
    }
}

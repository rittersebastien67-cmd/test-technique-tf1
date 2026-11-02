using HrAdministration.Domain.Balances;
using SharedKernel;

namespace HrAdministration.Domain.Tests.Balances;

/// <summary>Given past events, when a command runs, then it produces new events or an error.</summary>
public sealed class LeaveBalanceTests
{
    private static readonly LeaveBalanceId PaidLeave2026 = new(Guid.NewGuid(), BalanceType.PaidLeave, 2026);
    private static readonly LeaveBalanceId PaidLeave2027 = new(PaidLeave2026.EmployeeId, BalanceType.PaidLeave, 2027);
    private static readonly LeaveBalanceId Rtt2026 =new(Guid.NewGuid(), BalanceType.Rtt, 2026);
    private static readonly LeaveBalanceId Rtt2027 = new(Rtt2026.EmployeeId, BalanceType.Rtt, 2027);

    private static readonly Guid Request = Guid.NewGuid();
    private static readonly Guid OtherRequest = Guid.NewGuid();

    // The date used by the examples of R9.
    private static readonly DateOnly Today = new(2026, 11, 1);
    private static readonly DateOnly NextWeek = new(2026, 11, 9);
    private static readonly DateOnly NextWeekEnd = new(2026, 11, 13);
    private static readonly DateOnly LastMonth = new(2026, 10, 5);

    // Entitlement.

    [Fact]
    public void Granting_raises_the_entitlement()
    {
        var balance = LeaveBalance.Grant(PaidLeave2026, 25m, currentYearOnly: false);

        Assert.Equal([new EntitlementGranted(PaidLeave2026, 25m, false)], balance.UncommittedEvents);
    }

    [Fact]
    public void A_new_counter_is_fully_available()
    {
        var balance = Given(new EntitlementGranted(PaidLeave2026, 25m, false));

        Assert.Equal(PaidLeave2026, balance.Id);
        Assert.Equal(25m, balance.Acquired);
        Assert.Equal(0m, balance.Reserved);
        Assert.Equal(25m, balance.Available);
    }

    [Fact]
    public void A_negative_entitlement_is_a_bug()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LeaveBalance.Grant(PaidLeave2026, -1m, currentYearOnly: false));
    }

    // Deduction and solvency (R3).

    [Fact]
    public void Deducting_raises_the_days_of_the_request()
    {
        var balance = Given(new EntitlementGranted(PaidLeave2026, 25m, false));

        var result = balance.Deduct(Request, 5m, NextWeek, NextWeekEnd, Today);

        Assert.True(result.IsSuccess);
        Assert.Equal([new DaysDeducted(Request, 5m, NextWeek)], balance.UncommittedEvents);
        Assert.Equal(5m, balance.Reserved);
        Assert.Equal(20m, balance.Available);
    }

    [Fact]
    public void The_whole_available_balance_can_be_deducted()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 5m, false),
            new DaysDeducted(OtherRequest, 2.5m, LastMonth));

        var result = balance.Deduct(Request, 2.5m, NextWeek, NextWeekEnd, Today);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, balance.Available);
    }

    [Fact]
    public void An_insufficient_balance_is_refused_with_the_figures()
    {
        var balance = Given(
            new EntitlementGranted(Rtt2026, 10m, false),
            new DaysDeducted(OtherRequest, 8m, LastMonth));

        var result = balance.Deduct(Request, 3m, NextWeek, NextWeekEnd, Today);

        Assert.Equal("balance.insufficient", result.Error?.Code);
        Assert.Equal("Solde RTT insuffisant : 3 j demandés, 2 j disponibles.", result.Error?.Message);
        Assert.Empty(balance.UncommittedEvents);
    }

    [Fact]
    public void Half_a_day_too_many_is_refused()
    {
        var balance = Given(new EntitlementGranted(PaidLeave2026, 1m, false));

        var result = balance.Deduct(Request, 1.5m, NextWeek, NextWeekEnd, Today);

        Assert.Equal("Solde CP insuffisant : 1,5 j demandé, 1 j disponible.", result.Error?.Message);
    }

    [Fact]
    public void A_counter_without_entitlement_refuses_any_deduction()
    {
        var balance = Given(new EntitlementGranted(Rtt2026, 0m, false));

        var result = balance.Deduct(Request, 0.5m, NextWeek, NextWeek, Today);

        Assert.Equal("balance.insufficient", result.Error?.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deducting_no_day_is_a_bug(int days)
    {
        var balance = Given(new EntitlementGranted(PaidLeave2026, 25m, false));

        Assert.Throws<ArgumentOutOfRangeException>(() => balance.Deduct(Request, days, NextWeek, NextWeekEnd, Today));
    }

    // One deduction per request.

    [Fact]
    public void Deducting_a_request_again_raises_nothing()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 5m, NextWeek));

        var result = balance.Deduct(Request, 5m, NextWeek, NextWeekEnd, Today);

        Assert.True(result.IsSuccess);
        Assert.Empty(balance.UncommittedEvents);
        Assert.Equal(5m, balance.Reserved);
    }

    [Fact]
    public void Replaying_a_deduction_succeeds_even_when_the_balance_is_now_too_low()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 5m, false),
            new DaysDeducted(Request, 5m, NextWeek));

        var result = balance.Deduct(Request, 5m, NextWeek, NextWeekEnd, Today);

        Assert.True(result.IsSuccess);
        Assert.Empty(balance.UncommittedEvents);
    }

    [Fact]
    public void A_fully_credited_request_cannot_be_deducted_again()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 5m, NextWeek),
            new DaysCredited(Request, 5m));

        var result = balance.Deduct(Request, 5m, NextWeek, NextWeekEnd, Today);

        Assert.True(result.IsSuccess);
        Assert.Empty(balance.UncommittedEvents);
        Assert.Equal(25m, balance.Available);
    }

    [Fact]
    public void Two_requests_are_deducted_separately()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(OtherRequest, 5m, NextWeek));

        balance.Deduct(Request, 5m, NextWeek, NextWeekEnd, Today);

        Assert.Equal(10m, balance.Reserved);
    }

    // Counter limited to the current year (R9).

    [Fact]
    public void A_current_year_counter_accepts_a_period_within_the_year()
    {
        var balance = Given(new EntitlementGranted(Rtt2026, 10m, true));

        var result = balance.Deduct(Request, 5m, NextWeek, NextWeekEnd, Today);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void A_current_year_counter_of_next_year_refuses_august_2027_on_november_1st_2026()
    {
        var balance = Given(new EntitlementGranted(Rtt2027, 10m, true));

        var result = balance.Deduct(Request, 5m, new DateOnly(2027, 8, 2), new DateOnly(2027, 8, 6), Today);

        Assert.Equal("jrtt.outside_year", result.Error?.Code);
        Assert.Empty(balance.UncommittedEvents);
    }

    [Fact]
    public void A_current_year_counter_refuses_a_period_ending_next_year()
    {
        var balance = Given(new EntitlementGranted(Rtt2026, 10m, true));

        var result = balance.Deduct(Request, 3m, new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 4), Today);

        Assert.Equal("jrtt.outside_year", result.Error?.Code);
    }

    [Fact]
    public void A_current_year_counter_refuses_a_period_starting_the_year_before()
    {
        var balance = Given(new EntitlementGranted(Rtt2027, 10m, true));

        var result = balance.Deduct(
            Request,
            3m,
            new DateOnly(2026, 12, 30),
            new DateOnly(2027, 1, 4),
            today: new DateOnly(2027, 1, 2));

        Assert.Equal("jrtt.outside_year", result.Error?.Code);
    }

    [Fact]
    public void A_current_year_counter_becomes_usable_when_its_year_begins()
    {
        var balance = Given(new EntitlementGranted(Rtt2027, 10m, true));

        var result = balance.Deduct(
            Request,
            5m,
            new DateOnly(2027, 8, 2),
            new DateOnly(2027, 8, 6),
            today: new DateOnly(2027, 1, 1));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void A_current_year_counter_expires_with_its_year_even_with_days_left()
    {
        var balance = Given(new EntitlementGranted(Rtt2026, 10m, true));

        var result = balance.Deduct(
            Request,
            1m,
            new DateOnly(2027, 1, 4),
            new DateOnly(2027, 1, 4),
            today: new DateOnly(2027, 1, 2));

        Assert.Equal("jrtt.outside_year", result.Error?.Code);
    }

    [Fact]
    public void The_year_is_checked_before_the_balance()
    {
        var balance = Given(new EntitlementGranted(Rtt2027, 1m, true));

        var result = balance.Deduct(Request, 5m, new DateOnly(2027, 8, 2), new DateOnly(2027, 8, 6), Today);

        Assert.Equal("jrtt.outside_year", result.Error?.Code);
    }

    [Fact]
    public void A_regular_counter_can_plan_a_leave_of_next_year()
    {
        var balance = Given(new EntitlementGranted(PaidLeave2027, 25m, false));

        var result = balance.Deduct(Request, 5m, new DateOnly(2027, 8, 2), new DateOnly(2027, 8, 6), Today);

        Assert.True(result.IsSuccess);
    }

    // A period belongs to a single year, and to the counter of that year.

    [Fact]
    public void Paid_leave_spanning_two_years_is_refused()
    {
        var balance = Given(new EntitlementGranted(PaidLeave2026, 25m, false));

        var result = balance.Deduct(Request, 3m, new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 4), Today);

        Assert.Equal("period.spans_two_years", result.Error?.Code);
        Assert.Equal(
            "Une demande de CP ne peut pas être à cheval sur deux années civiles : posez une demande par année.",
            result.Error?.Message);
        Assert.Empty(balance.UncommittedEvents);
    }

    [Fact]
    public void Rtt_spanning_two_years_is_refused()
    {
        var balance = Given(new EntitlementGranted(Rtt2026, 10m, false));

        var result = balance.Deduct(Request, 3m, new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 4), Today);

        Assert.Equal("period.spans_two_years", result.Error?.Code);
    }

    [Fact]
    public void A_period_from_december_31st_to_january_1st_spans_two_years()
    {
        var balance = Given(new EntitlementGranted(PaidLeave2026, 25m, false));

        var result = balance.Deduct(Request, 1m, new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 1), Today);

        Assert.Equal("period.spans_two_years", result.Error?.Code);
    }

    [Fact]
    public void A_period_ending_on_december_31st_is_accepted()
    {
        var balance = Given(new EntitlementGranted(PaidLeave2026, 25m, false));

        var result = balance.Deduct(Request, 2m, new DateOnly(2026, 12, 30), new DateOnly(2026, 12, 31), Today);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void A_period_starting_on_january_1st_is_accepted()
    {
        var balance = Given(new EntitlementGranted(PaidLeave2027, 25m, false));

        var result = balance.Deduct(Request, 1m, new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 4), Today);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Two_years_are_checked_before_the_balance()
    {
        var balance = Given(new EntitlementGranted(PaidLeave2026, 1m, false));

        var result = balance.Deduct(Request, 3m, new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 4), Today);

        Assert.Equal("period.spans_two_years", result.Error?.Code);
    }

    [Fact]
    public void A_period_of_another_year_is_refused_by_the_counter()
    {
        var balance = Given(new EntitlementGranted(PaidLeave2026, 25m, false));

        var result = balance.Deduct(Request, 5m, new DateOnly(2027, 8, 2), new DateOnly(2027, 8, 6), Today);

        Assert.Equal("period.outside_counter_year", result.Error?.Code);
        Assert.Equal("Cette période ne relève pas du compteur CP 2026.", result.Error?.Message);
        Assert.Empty(balance.UncommittedEvents);
    }

    // Taken and planned days.

    [Fact]
    public void A_period_to_come_is_planned()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 5m, NextWeek));

        Assert.Equal(0m, balance.Taken(Today));
        Assert.Equal(5m, balance.Planned(Today));
    }

    [Fact]
    public void A_period_over_is_taken()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 5m, LastMonth));

        Assert.Equal(5m, balance.Taken(Today));
        Assert.Equal(0m, balance.Planned(Today));
    }

    [Fact]
    public void A_period_in_progress_is_taken_entirely()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 5m, NextWeek));

        Assert.Equal(5m, balance.Taken(NextWeek));
        Assert.Equal(0m, balance.Planned(NextWeek));
    }

    [Fact]
    public void Planned_days_become_taken_as_time_goes_by()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 5m, NextWeek));

        Assert.Equal(5m, balance.Planned(NextWeek.AddDays(-1)));
        Assert.Equal(5m, balance.Taken(NextWeek));
    }

    [Fact]
    public void Taken_and_planned_add_up_to_reserved()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(OtherRequest, 3m, LastMonth),
            new DaysDeducted(Request, 5m, NextWeek));

        Assert.Equal(3m, balance.Taken(Today));
        Assert.Equal(5m, balance.Planned(Today));
        Assert.Equal(8m, balance.Reserved);
        Assert.Equal(17m, balance.Available);
    }

    // Credit (R7, R10).

    [Fact]
    public void Crediting_a_cancelled_request_frees_its_days()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 5m, NextWeek));

        balance.Credit(Request, 5m);

        Assert.Equal([new DaysCredited(Request, 5m)], balance.UncommittedEvents);
        Assert.Equal(0m, balance.Reserved);
        Assert.Equal(25m, balance.Available);
    }

    [Fact]
    public void Crediting_part_of_a_request_leaves_the_rest_reserved()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 5m, NextWeek));

        balance.Credit(Request, 1.5m);

        Assert.Equal(3.5m, balance.Planned(Today));
        Assert.Equal(21.5m, balance.Available);
    }

    [Fact]
    public void A_started_leave_can_be_credited_and_its_taken_days_decrease()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 5m, LastMonth));

        balance.Credit(Request, 2m);

        Assert.Equal(3m, balance.Taken(Today));
    }

    [Fact]
    public void Credited_days_can_be_deducted_again_by_another_request()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 5m, false),
            new DaysDeducted(OtherRequest, 5m, LastMonth),
            new DaysCredited(OtherRequest, 2m));

        var result = balance.Deduct(Request, 2m, NextWeek, NextWeekEnd, Today);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, balance.Available);
    }

    [Fact]
    public void Successive_credits_are_limited_to_what_remains()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 5m, NextWeek),
            new DaysCredited(Request, 4m));

        Assert.Throws<InvalidOperationException>(() => balance.Credit(Request, 1.5m));
        Assert.Empty(balance.UncommittedEvents);
    }

    [Fact]
    public void Crediting_an_unknown_request_is_a_bug()
    {
        var balance = Given(new EntitlementGranted(PaidLeave2026, 25m, false));

        Assert.Throws<InvalidOperationException>(() => balance.Credit(Request, 1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Crediting_no_day_is_a_bug(int days)
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 5m, NextWeek));

        Assert.Throws<ArgumentOutOfRangeException>(() => balance.Credit(Request, days));
    }

    // Part carried over after a sick leave override (R10).

    [Fact]
    public void Carrying_over_reserves_the_days_on_the_first_day_of_the_part()
    {
        var firstDay = NextWeek.AddDays(3);
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(OtherRequest, 5m, NextWeek),
            new DaysCredited(OtherRequest, 5m));

        balance.DeductCarriedOver(Request, 2m, firstDay);

        Assert.Equal([new DaysDeducted(Request, 2m, firstDay)], balance.UncommittedEvents);
        Assert.Equal(2m, balance.Planned(NextWeek));
        Assert.Equal(2m, balance.Taken(firstDay));
    }

    [Fact]
    public void A_counter_of_the_current_year_accepts_a_part_after_its_year_is_over()
    {
        var balance = Given(
            new EntitlementGranted(Rtt2026, 10m, true),
            new DaysDeducted(OtherRequest, 3m, new DateOnly(2026, 12, 29)),
            new DaysCredited(OtherRequest, 3m));

        balance.DeductCarriedOver(Request, 1m, new DateOnly(2026, 12, 31));

        Assert.Equal(1m, balance.Reserved);
    }

    [Fact]
    public void A_part_carried_over_again_raises_nothing()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 2m, NextWeek));

        balance.DeductCarriedOver(Request, 2m, NextWeek);

        Assert.Empty(balance.UncommittedEvents);
        Assert.Equal(2m, balance.Reserved);
    }

    [Fact]
    public void A_part_can_be_credited_like_any_request()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 25m, false),
            new DaysDeducted(Request, 2m, NextWeek));

        balance.Credit(Request, 2m);

        Assert.Equal(25m, balance.Available);
    }

    [Fact]
    public void Carrying_over_before_the_credit_is_a_bug()
    {
        var balance = Given(
            new EntitlementGranted(PaidLeave2026, 5m, false),
            new DaysDeducted(OtherRequest, 5m, NextWeek));

        Assert.Throws<InvalidOperationException>(() => balance.DeductCarriedOver(Request, 2m, NextWeek));
        Assert.Empty(balance.UncommittedEvents);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Carrying_over_no_day_is_a_bug(int days)
    {
        var balance = Given(new EntitlementGranted(PaidLeave2026, 25m, false));

        Assert.Throws<ArgumentOutOfRangeException>(() => balance.DeductCarriedOver(Request, days, NextWeek));
    }

    // Replay.

    [Fact]
    public void Replaying_the_events_gives_the_same_counter()
    {
        var balance = LeaveBalance.Grant(Rtt2026, 10m, currentYearOnly: true);
        balance.Deduct(Request, 5m, NextWeek, NextWeekEnd, Today);
        balance.Credit(Request, 2m);

        var replayed = LeaveBalance.FromHistory(balance.UncommittedEvents);

        Assert.Equal(balance.Id, replayed.Id);
        Assert.True(replayed.CurrentYearOnly);
        Assert.Equal(10m, replayed.Acquired);
        Assert.Equal(3m, replayed.Planned(Today));
        Assert.Equal(7m, replayed.Available);
        Assert.Equal(3, replayed.Version);
        Assert.Empty(replayed.UncommittedEvents);
    }

    private static LeaveBalance Given(params IDomainEvent[] history) => LeaveBalance.FromHistory(history);
}

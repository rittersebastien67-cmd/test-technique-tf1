using HrAdministration.Application.EventSourcing;
using HrAdministration.Domain.Balances;

namespace HrAdministration.Application.Tests.EventSourcing;

public sealed class StreamIdsTests
{
    private static readonly Guid Id = Guid.Parse("7f3b5c1e-0d7a-4f60-9a55-0a4f8a3c1b11");

    [Fact]
    public void A_leave_request_has_its_own_stream()
    {
        Assert.Equal("leave-request-7f3b5c1e-0d7a-4f60-9a55-0a4f8a3c1b11", StreamIds.LeaveRequest(Id));
    }

    [Fact]
    public void A_counter_has_a_stream_per_employee_type_and_year()
    {
        Assert.Equal(
            "balance-7f3b5c1e-0d7a-4f60-9a55-0a4f8a3c1b11-PaidLeave-2026",
            StreamIds.Balance(new LeaveBalanceId(Id, BalanceType.PaidLeave, 2026)));
    }

    [Fact]
    public void An_employee_has_one_agenda()
    {
        Assert.Equal("agenda-7f3b5c1e-0d7a-4f60-9a55-0a4f8a3c1b11", StreamIds.Agenda(Id));
    }
}

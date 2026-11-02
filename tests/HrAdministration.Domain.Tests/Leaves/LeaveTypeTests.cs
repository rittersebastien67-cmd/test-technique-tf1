using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

public sealed class LeaveTypeTests
{
    [Fact]
    public void Only_sick_leave_is_sensitive()
    {
        Assert.True(LeaveType.SickLeave.IsSensitive);
        Assert.False(LeaveType.PaidLeave.IsSensitive);
        Assert.False(LeaveType.Rtt.IsSensitive);
        Assert.False(LeaveType.UnpaidLeave.IsSensitive);
    }
}

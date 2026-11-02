using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

public sealed class LeaveTypeTests
{
    [Fact]
    public void Only_sick_leave_is_sensitive()
    {
        Assert.True(new LeaveTypeSickLeave().IsSensitive);
        Assert.False(new LeaveTypePaidLeave().IsSensitive);
        Assert.False(new LeaveTypeRtt().IsSensitive);
        Assert.False(new LeaveTypeUnpaidLeave().IsSensitive);
    }

    [Fact]
    public void Each_type_has_a_stable_name()
    {
        Assert.Equal("PaidLeave", new LeaveTypePaidLeave().Name);
        Assert.Equal("Rtt", new LeaveTypeRtt().Name);
        Assert.Equal("SickLeave", new LeaveTypeSickLeave().Name);
        Assert.Equal("UnpaidLeave", new LeaveTypeUnpaidLeave().Name);
    }

    [Fact]
    public void Two_instances_of_a_type_are_equal()
    {
        LeaveType first = new LeaveTypeRtt();
        LeaveType second = new LeaveTypeRtt();

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Two_different_types_are_not_equal()
    {
        LeaveType rtt = new LeaveTypeRtt();
        LeaveType paidLeave = new LeaveTypePaidLeave();

        Assert.NotEqual(rtt, paidLeave);
    }

    [Fact]
    public void To_string_is_the_name()
    {
        Assert.Equal("Rtt", new LeaveTypeRtt().ToString());
    }
}

using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

public sealed class BackdatingPolicyTests
{
    private static readonly DateOnly Today = new(2026, 11, 5);

    private static readonly LeaveType[] TypesWithoutBackdating = [new LeaveTypePaidLeave(), new LeaveTypeRtt(), new LeaveTypeUnpaidLeave()];

    [Fact]
    public void Any_type_can_start_today_or_later()
    {
        LeaveType[] types = [.. TypesWithoutBackdating, new LeaveTypeSickLeave()];

        Assert.All(types, type =>
        {
            Assert.Null(BackdatingPolicy.Check(Today, type, Today).Value);
            Assert.Null(BackdatingPolicy.Check(Today.AddDays(1), type, Today).Value);
        });
    }

    [Fact]
    public void Only_a_sick_leave_can_start_in_the_past()
    {
        Assert.All(TypesWithoutBackdating, type =>
        {
            var result = BackdatingPolicy.Check(Today.AddDays(-1), type, Today);

            Assert.Equal("backdating.forbidden", result.Error?.Code);
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void A_sick_leave_declared_within_two_days_has_no_warning(int daysAgo)
    {
        var result = BackdatingPolicy.Check(Today.AddDays(-daysAgo), new LeaveTypeSickLeave(), Today);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(30)]
    public void A_sick_leave_declared_later_is_accepted_with_a_warning(int daysAgo)
    {
        var result = BackdatingPolicy.Check(Today.AddDays(-daysAgo), new LeaveTypeSickLeave(), Today);

        Assert.True(result.IsSuccess);
        Assert.Equal("declaration.late", result.Value?.Code);
    }
}

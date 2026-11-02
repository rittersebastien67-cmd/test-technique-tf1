using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

public sealed class PeriodBoundTests
{
    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly DateOnly Tuesday = new(2026, 11, 17);

    [Fact]
    public void The_half_day_before_an_afternoon_is_the_morning_of_the_same_day()
    {
        Assert.Equal(new PeriodBound(Tuesday, HalfDay.Morning), new PeriodBound(Tuesday, HalfDay.Afternoon).PreviousHalfDay());
    }

    [Fact]
    public void The_half_day_before_a_morning_is_the_afternoon_of_the_day_before()
    {
        Assert.Equal(new PeriodBound(Monday, HalfDay.Afternoon), new PeriodBound(Tuesday, HalfDay.Morning).PreviousHalfDay());
    }

    [Fact]
    public void The_half_day_after_a_morning_is_the_afternoon_of_the_same_day()
    {
        Assert.Equal(new PeriodBound(Monday, HalfDay.Afternoon), new PeriodBound(Monday, HalfDay.Morning).NextHalfDay());
    }

    [Fact]
    public void The_half_day_after_an_afternoon_is_the_morning_of_the_next_day()
    {
        Assert.Equal(new PeriodBound(Tuesday, HalfDay.Morning), new PeriodBound(Monday, HalfDay.Afternoon).NextHalfDay());
    }
}

using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

public sealed class LeavePeriodTests
{
    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly DateOnly Tuesday = new(2026, 11, 17);

    [Theory]
    [InlineData(HalfDay.Morning, HalfDay.Morning)]
    [InlineData(HalfDay.Afternoon, HalfDay.Afternoon)]
    [InlineData(HalfDay.Morning, HalfDay.Afternoon)]
    public void A_single_day_is_valid_when_the_start_is_not_after_the_end(HalfDay start, HalfDay end)
    {
        var result = LeavePeriod.Create(new PeriodBound(Monday, start), new PeriodBound(Monday, end));

        Assert.True(result.IsSuccess);
        Assert.Equal(new PeriodBound(Monday, start), result.Value.Start);
        Assert.Equal(new PeriodBound(Monday, end), result.Value.End);
    }

    [Fact]
    public void Afternoon_to_morning_of_the_same_day_is_invalid()
    {
        var result = LeavePeriod.Create(new PeriodBound(Monday, HalfDay.Afternoon), new PeriodBound(Monday, HalfDay.Morning));

        Assert.Equal("period.invalid", result.Error?.Code);
    }

    [Theory]
    [InlineData(HalfDay.Morning, HalfDay.Morning)]
    [InlineData(HalfDay.Afternoon, HalfDay.Morning)]
    [InlineData(HalfDay.Morning, HalfDay.Afternoon)]
    [InlineData(HalfDay.Afternoon, HalfDay.Afternoon)]
    public void Over_several_days_the_half_days_do_not_matter(HalfDay start, HalfDay end)
    {
        var result = LeavePeriod.Create(new PeriodBound(Monday, start), new PeriodBound(Tuesday, end));

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(HalfDay.Morning, HalfDay.Afternoon)]
    [InlineData(HalfDay.Afternoon, HalfDay.Morning)]
    public void A_start_on_a_later_day_is_invalid(HalfDay start, HalfDay end)
    {
        var result = LeavePeriod.Create(new PeriodBound(Tuesday, start), new PeriodBound(Monday, end));

        Assert.Equal("period.invalid", result.Error?.Code);
    }

    [Fact]
    public void Periods_with_the_same_bounds_are_equal()
    {
        var start = new PeriodBound(Monday, HalfDay.Morning);
        var end = new PeriodBound(Tuesday, HalfDay.Afternoon);

        Assert.Equal(LeavePeriod.Create(start, end).Value, LeavePeriod.Create(start, end).Value);
    }
}

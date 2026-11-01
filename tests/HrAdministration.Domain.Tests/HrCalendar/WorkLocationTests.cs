using HrAdministration.Domain.HrCalendar;

namespace HrAdministration.Domain.Tests.HrCalendar;

public sealed class WorkLocationTests
{
    [Theory]
    [InlineData("01")]
    [InlineData("2A")]
    [InlineData("2B")]
    [InlineData("57")]
    [InlineData("95")]
    [InlineData("976")]
    public void French_department_codes_are_accepted(string department)
    {
        Assert.Equal(department, new WorkLocation(department).Department);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("00")]
    [InlineData("20")]
    [InlineData("2C")]
    [InlineData("96")]
    [InlineData("977")]
    [InlineData("ABC")]
    public void Unknown_department_codes_are_rejected(string department)
    {
        Assert.Throws<ArgumentException>(() => new WorkLocation(department));
    }

}

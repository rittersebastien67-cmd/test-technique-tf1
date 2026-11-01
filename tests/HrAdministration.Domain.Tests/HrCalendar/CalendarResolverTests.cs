using HrAdministration.Domain.HrCalendar;

namespace HrAdministration.Domain.Tests.HrCalendar;

public sealed class CalendarResolverTests
{
    [Theory]
    [InlineData("57")]
    [InlineData("67")]
    [InlineData("68")]
    public void Alsace_Moselle_departments_get_the_alsace_moselle_calendar(string department)
    {
        Assert.Same(PublicHolidayCalendar.AlsaceMoselle, CalendarResolver.Resolve(new WorkLocation(department)));
    }

    [Theory]
    [InlineData("75")]
    [InlineData("54")]
    [InlineData("88")]
    [InlineData("2A")]
    [InlineData("971")]
    public void Other_departments_get_the_general_calendar(string department)
    {
        Assert.Same(PublicHolidayCalendar.General, CalendarResolver.Resolve(new WorkLocation(department)));
    }
}

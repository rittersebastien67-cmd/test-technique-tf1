using HrAdministration.Domain.HrCalendar;

namespace HrAdministration.Domain.Tests.HrCalendar;

public sealed class WorkScheduleTests
{
    [Theory]
    [InlineData(DayOfWeek.Monday, true)]
    [InlineData(DayOfWeek.Wednesday, true)]
    [InlineData(DayOfWeek.Friday, true)]
    [InlineData(DayOfWeek.Saturday, false)]
    [InlineData(DayOfWeek.Sunday, false)]
    public void Full_time_works_monday_to_friday(DayOfWeek day, bool worked)
    {
        Assert.Equal(worked, WorkSchedule.FullTime5Days.IsWorked(day));
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, true)]
    [InlineData(DayOfWeek.Tuesday, true)]
    [InlineData(DayOfWeek.Wednesday, false)]
    [InlineData(DayOfWeek.Thursday, true)]
    [InlineData(DayOfWeek.Friday, true)]
    [InlineData(DayOfWeek.Saturday, false)]
    [InlineData(DayOfWeek.Sunday, false)]
    public void Part_time_80_does_not_work_on_wednesday(DayOfWeek day, bool worked)
    {
        Assert.Equal(worked, WorkSchedule.PartTime80.IsWorked(day));
    }
}

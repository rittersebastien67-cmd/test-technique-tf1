using SharedKernel;

namespace HrAdministration.Domain.Leaves;

/// <summary>
/// Counts the days of a leave by steps of 0.5 (R2). Only working days count, and a zero duration is invalid.
/// Paid leave follows the legal count (PRD 4.1): it runs up to the day before the return to work, non-working days included.
/// Other types only count worked days (PRD D39).
/// </summary>
public static class CountingPolicy
{
    private const decimal FullDay = 1m;
    private const decimal HalfDayLength = 0.5m;

    public static Result<CountedDuration> Count(LeavePeriod period, LeaveType type, ILeaveCalendar calendar)
    {
        var countsNonWorkingDays = type is LeaveTypePaidLeave;
        var previousWorkedDayIsFull = false;
        var days = new List<CountedDay>();

        for (var date = period.Start.Date; ; date = date.AddDays(1))
        {
            var kind = calendar.Qualify(date);
            var isAfterPeriod = date > period.End.Date;

            // Only paid leave goes past the period, and at most up to the return to work: the first worked day after it.
            if (isAfterPeriod && (kind == DayKind.WorkingDay || !countsNonWorkingDays))
            {
                break;
            }

            var counted = 0m;
            if (kind == DayKind.WorkingDay)
            {
                counted = LengthOn(date, period);
                previousWorkedDayIsFull = counted == FullDay;
            }
            else if (kind == DayKind.NonWorkingDay && countsNonWorkingDays && previousWorkedDayIsFull)
            {
                // A half-day does not extend to the next non-working day (PRD D40).
                counted = FullDay;
            }

            if (!isAfterPeriod || counted > 0)
            {
                days.Add(new CountedDay(date, kind, counted));
            }
        }

        var duration = new CountedDuration(days);
        return duration.Total == 0 ? LeaveErrors.DurationZero : duration;
    }

    private static decimal LengthOn(DateOnly date, LeavePeriod period)
    {
        var startsInAfternoon = date == period.Start.Date && period.Start.HalfDay == HalfDay.Afternoon;
        var endsInMorning = date == period.End.Date && period.End.HalfDay == HalfDay.Morning;

        // Afternoon→Morning of a single day is not a valid period, so at most one half is missing.
        return startsInAfternoon || endsInMorning ? HalfDayLength : FullDay;
    }
}

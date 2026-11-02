namespace HrAdministration.Domain.Leaves;

/// <summary>Port giving the nature of a day for the employee concerned. Implemented by the Application layer, as Domain modules do not know each other.</summary>
public interface ILeaveCalendar
{
    /// <summary>Whether <see cref="Qualify"/> can answer: the calendar only knows some years, and a request can name any date.</summary>
    bool Covers(DateOnly date);

    DayKind Qualify(DateOnly date);
}

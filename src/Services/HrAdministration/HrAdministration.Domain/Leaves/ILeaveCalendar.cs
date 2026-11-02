namespace HrAdministration.Domain.Leaves;

/// <summary>Port giving the nature of a day for the employee concerned. Implemented by the Application layer, as Domain modules do not know each other.</summary>
public interface ILeaveCalendar
{
    DayKind Qualify(DateOnly date);
}

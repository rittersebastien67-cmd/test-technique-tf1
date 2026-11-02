namespace HrAdministration.Domain.Leaves;

/// <summary>Smallest unit that can be requested (0.5 day). Declared in chronological order, which <see cref="PeriodBound"/> relies on.</summary>
public enum HalfDay
{
    Morning,
    Afternoon,
}

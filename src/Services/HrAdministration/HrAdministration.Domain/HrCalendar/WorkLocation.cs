using System.Text.RegularExpressions;

namespace HrAdministration.Domain.HrCalendar;

/// <summary>
/// Department of the establishment where the employee works, as written on the HR record.
/// It determines the public holiday calendar (R11). Comes from demo data only, so an unknown code is a programming error.
/// </summary>
public sealed partial record WorkLocation
{
    public WorkLocation(string department)
    {
        if (!DepartmentCode().IsMatch(department))
        {
            throw new ArgumentException($"'{department}' is not a French department code.", nameof(department));
        }

        Department = department;
    }

    /// <summary>INSEE code: 01 to 95 with 2A and 2B for Corsica, 971 to 976 overseas.</summary>
    public string Department { get; }

    public override string ToString() => Department;

    [GeneratedRegex(@"^(0[1-9]|1\d|2[1-9AB]|[3-8]\d|9[0-5]|97[1-6])$")]
    private static partial Regex DepartmentCode();
}

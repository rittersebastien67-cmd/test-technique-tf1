using System.Globalization;
using SharedKernel;

namespace HrAdministration.Domain.Balances;

/// <summary>Rule violations of the Balances module. Codes are part of the API (section 6 of the architecture), messages are shown to the employee.</summary>
public static class BalanceErrors
{
    public static readonly Error OutsideCurrentYear = new(
        "jrtt.outside_year",
        "Les JRTT ne peuvent être posés que sur l'année civile en cours.");

    public static Error Insufficient(BalanceType type, decimal requested, decimal available) => new(
        "balance.insufficient",
        $"Solde {Label(type)} insuffisant : {Days(requested, "demandé")}, {Days(available, "disponible")}.");

    public static Error SpansTwoYears(BalanceType type) => new(
        "period.spans_two_years",
        $"Une demande de {Label(type)} ne peut pas être à cheval sur deux années civiles : posez une demande par année.");

    public static Error OutsideCounterYear(LeaveBalanceId id) => new(
        "period.outside_counter_year",
        $"Cette période ne relève pas du compteur {Label(id.Type)} {id.Year}.");

    private static string Label(BalanceType type) => type == BalanceType.PaidLeave ? "CP" : "RTT";

    /// <summary>Written by hand rather than with the fr-FR culture, which is missing from images without ICU.</summary>
    private static string Days(decimal days, string adjective)
    {
        var number = days.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');

        return $"{number} j {adjective}{(days >= 2m ? "s" : "")}";
    }
}

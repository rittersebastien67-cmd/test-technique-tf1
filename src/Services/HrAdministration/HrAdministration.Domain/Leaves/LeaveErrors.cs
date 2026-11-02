using System.Globalization;
using SharedKernel;

namespace HrAdministration.Domain.Leaves;

/// <summary>Rule violations of the Leaves module. Codes are part of the API (section 6 of the architecture), messages are shown to the employee.</summary>
public static class LeaveErrors
{
    public static readonly Error PeriodInvalid = new(
        "period.invalid",
        "La borne de début doit être antérieure ou égale à la borne de fin.");

    public static readonly Error PeriodOutsideCalendar = new(
        "period.outside_calendar",
        "Le calendrier de l'application ne couvre pas cette période.");

    public static readonly Error DurationZero = new(
        "duration.zero",
        "La période ne contient aucun jour à décompter.");

    public static readonly Error CommentTooLong = new(
        "comment.too_long",
        $"Le commentaire ne peut pas dépasser {Comment.MaxLength} caractères.");

    public static readonly Error CommentForbiddenForSickLeave = new(
        "comment.forbidden_for_sick_leave",
        "Un arrêt maladie ne peut pas comporter de commentaire.");

    public static readonly Error BackdatingForbidden = new(
        "backdating.forbidden",
        "La date de début ne peut pas être passée, sauf pour un arrêt maladie.");

    public static readonly Error LeaveStarted = new(
        "cancellation.leave_started",
        "Ce congé a déjà commencé : il ne peut plus être annulé.");

    public static readonly Error InvalidStatus = new(
        "request.invalid_status",
        "Cette opération n'est pas possible dans le statut actuel de la demande.");

    public static Error Overlap(DateOnly start, DateOnly end) => new(
        "period.overlap",
        $"Chevauche la demande du {DayAndMonth(start)} au {DayAndMonth(end)}.");

    private static string DayAndMonth(DateOnly date) => date.ToString("dd/MM", CultureInfo.InvariantCulture);
}

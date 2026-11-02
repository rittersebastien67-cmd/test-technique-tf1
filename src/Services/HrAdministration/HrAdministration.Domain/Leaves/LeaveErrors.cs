using SharedKernel;

namespace HrAdministration.Domain.Leaves;

/// <summary>Rule violations of the Leaves module. Codes are part of the API (section 6 of the architecture), messages are shown to the employee.</summary>
public static class LeaveErrors
{
    public static readonly Error PeriodInvalid = new(
        "period.invalid",
        "La borne de début doit être antérieure ou égale à la borne de fin.");

    public static readonly Error DurationZero = new(
        "duration.zero",
        "La période ne contient aucun jour à décompter.");

    public static readonly Error CommentTooLong = new(
        "comment.too_long",
        $"Le commentaire ne peut pas dépasser {Comment.MaxLength} caractères.");

    public static readonly Error CommentForbiddenForSickLeave = new(
        "comment.forbidden_for_sick_leave",
        "Un arrêt maladie ne peut pas comporter de commentaire.");
}

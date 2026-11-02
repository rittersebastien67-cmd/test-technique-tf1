using SharedKernel;

namespace HrAdministration.Domain.Leaves;

/// <summary>Optional free text of a leave request (R6): 200 characters at most, and none for sensitive types (C15).</summary>
public sealed record Comment
{
    public const int MaxLength = 200;

    private Comment(string text)
    {
        Text = text;
    }

    public string Text { get; }

    /// <summary>A missing or blank text is not a comment: the result is a success holding <c>null</c>, whatever the leave type.</summary>
    public static Result<Comment?> Create(string? text, LeaveType type)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Success<Comment?>(null);
        }

        if (type.IsSensitive)
        {
            return LeaveErrors.CommentForbiddenForSickLeave;
        }

        var trimmed = text.Trim();
        return trimmed.Length > MaxLength
            ? LeaveErrors.CommentTooLong
            : new Comment(trimmed);
    }

    /// <summary>Free text must never reach the logs (C3), even through an interpolated string.</summary>
    public override string ToString() => nameof(Comment);
}

using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

public sealed class CommentTests
{
    private static readonly LeaveType[] TypesWithComment = [LeaveType.PaidLeave, LeaveType.Rtt, LeaveType.UnpaidLeave];

    [Fact]
    public void A_comment_is_accepted_on_non_sensitive_types()
    {
        Assert.All(TypesWithComment, type =>
        {
            var result = Comment.Create("Vacances en famille", type);

            Assert.Equal("Vacances en famille", result.Value?.Text);
        });
    }

    [Fact]
    public void A_comment_of_200_characters_is_accepted()
    {
        var result = Comment.Create(new string('a', 200), LeaveType.PaidLeave);

        Assert.Equal(200, result.Value?.Text.Length);
    }

    [Fact]
    public void A_comment_of_201_characters_is_rejected()
    {
        var result = Comment.Create(new string('a', 201), LeaveType.PaidLeave);

        Assert.Equal("comment.too_long", result.Error?.Code);
    }

    [Fact]
    public void Surrounding_whitespace_is_neither_kept_nor_counted()
    {
        var text = new string('a', 200);

        var result = Comment.Create($"  {text}  ", LeaveType.PaidLeave);

        Assert.Equal(text, result.Value?.Text);
    }

    [Fact]
    public void A_comment_is_refused_for_sick_leave()
    {
        var result = Comment.Create("Grippe", LeaveType.SickLeave);

        Assert.Equal("comment.forbidden_for_sick_leave", result.Error?.Code);
    }

    [Fact]
    public void A_too_long_comment_for_sick_leave_is_reported_as_forbidden()
    {
        var result = Comment.Create(new string('a', 201), LeaveType.SickLeave);

        Assert.Equal("comment.forbidden_for_sick_leave", result.Error?.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_text_is_no_comment_whatever_the_type(string? text)
    {
        Assert.All([.. TypesWithComment, LeaveType.SickLeave], type =>
        {
            var result = Comment.Create(text, type);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value);
        });
    }

    [Fact]
    public void To_string_does_not_reveal_the_text()
    {
        var comment = Comment.Create("Vacances en famille", LeaveType.PaidLeave).Value;

        Assert.DoesNotContain("Vacances", $"{comment}");
    }
}

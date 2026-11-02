using HrAdministration.Domain.Leaves;

namespace HrAdministration.Domain.Tests.Leaves;

public sealed class CommentTests
{
    private static readonly LeaveType[] TypesWithComment = [new LeaveTypePaidLeave(), new LeaveTypeRtt(), new LeaveTypeUnpaidLeave()];

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
        var result = Comment.Create(new string('a', 200), new LeaveTypePaidLeave());

        Assert.Equal(200, result.Value?.Text.Length);
    }

    [Fact]
    public void A_comment_of_201_characters_is_rejected()
    {
        var result = Comment.Create(new string('a', 201), new LeaveTypePaidLeave());

        Assert.Equal("comment.too_long", result.Error?.Code);
    }

    [Fact]
    public void Surrounding_whitespace_is_neither_kept_nor_counted()
    {
        var text = new string('a', 200);

        var result = Comment.Create($"  {text}  ", new LeaveTypePaidLeave());

        Assert.Equal(text, result.Value?.Text);
    }

    [Fact]
    public void A_comment_is_refused_for_sick_leave()
    {
        var result = Comment.Create("Grippe", new LeaveTypeSickLeave());

        Assert.Equal("comment.forbidden_for_sick_leave", result.Error?.Code);
    }

    [Fact]
    public void A_too_long_comment_for_sick_leave_is_reported_as_forbidden()
    {
        var result = Comment.Create(new string('a', 201), new LeaveTypeSickLeave());

        Assert.Equal("comment.forbidden_for_sick_leave", result.Error?.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_text_is_no_comment_whatever_the_type(string? text)
    {
        Assert.All([.. TypesWithComment, new LeaveTypeSickLeave()], type =>
        {
            var result = Comment.Create(text, type);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value);
        });
    }

    [Fact]
    public void To_string_does_not_reveal_the_text()
    {
        var comment = Comment.Create("Vacances en famille", new LeaveTypePaidLeave()).Value;

        Assert.DoesNotContain("Vacances", $"{comment}");
    }
}

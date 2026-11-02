using HrAdministration.Domain.Leaves;
using SharedKernel;

namespace HrAdministration.Domain.Tests.Leaves;

/// <summary>Given past events, when a command runs, then it produces new events or an error.</summary>
public sealed class LeaveRequestTests
{
    private static readonly Guid Request = Guid.NewGuid();
    private static readonly Guid Employee = Guid.NewGuid();

    private static readonly ILeaveCalendar Calendar = FakeLeaveCalendar.FullTime();

    // A Monday.
    private static readonly DateOnly Today = new(2026, 11, 2);
    private static readonly DateOnly NextMonday = new(2026, 11, 9);
    private static readonly DateOnly NextFriday = new(2026, 11, 13);
    private static readonly DateOnly NextSaturday = new(2026, 11, 7);
    private static readonly DateOnly NextSunday = new(2026, 11, 8);

    private static readonly DraftContent NextWeek = Content(new LeaveTypePaidLeave(), NextMonday, NextFriday);

    // Draft creation.

    [Fact]
    public void Drafting_raises_the_content_of_the_request()
    {
        var content = NextWeek with { Comment = "Vacances en famille" };

        var request = LeaveRequest.Draft(Request, Employee, Origin.Manual, content, Today, Calendar).Value;

        var drafted = Assert.IsType<LeaveRequestDrafted>(Assert.Single(request.UncommittedEvents));
        Assert.Equal(Request, drafted.LeaveRequestId);
        Assert.Equal(Employee, drafted.EmployeeId);
        Assert.Equal(Origin.Manual, drafted.Origin);
        Assert.Equal(new LeaveTypePaidLeave(), drafted.Type);
        Assert.Equal(new PeriodBound(NextMonday, HalfDay.Morning), drafted.Start);
        Assert.Equal(new PeriodBound(NextFriday, HalfDay.Afternoon), drafted.End);
        Assert.Equal("Vacances en famille", drafted.Comment?.Text);
        Assert.Equal(5m, drafted.Duration?.Total);
        Assert.Empty(drafted.Flags);
        Assert.Empty(drafted.Warnings);
    }

    [Fact]
    public void A_new_request_is_a_draft()
    {
        var request = Draft(NextWeek);

        Assert.Equal(Request, request.Id);
        Assert.Equal(Employee, request.EmployeeId);
        Assert.Equal(LeaveRequestStatus.Draft, request.Status);
        Assert.Equal(5m, request.Duration?.Total);
    }

    [Theory]
    [InlineData(Origin.Manual)]
    [InlineData(Origin.Agent)]
    public void A_draft_keeps_its_origin(Origin origin)
    {
        var request = LeaveRequest.Draft(Request, Employee, origin, NextWeek, Today, Calendar).Value;

        Assert.Equal(origin, request.Origin);
    }

    [Fact]
    public void Assumptions_of_the_agent_become_uncertainties()
    {
        var content = NextWeek with { Assumptions = ["YEAR_INFERRED", "TYPE_INFERRED"] };

        var request = LeaveRequest.Draft(Request, Employee, Origin.Agent, content, Today, Calendar).Value;

        Assert.Equal([Flag.Uncertainty("YEAR_INFERRED"), Flag.Uncertainty("TYPE_INFERRED")], request.Flags);
    }

    // A draft can be invalid: it is saved with its inconsistencies (A3).

    [Fact]
    public void A_draft_with_an_invalid_period_is_saved_with_an_inconsistency()
    {
        var content = Content(new LeaveTypePaidLeave(), NextFriday, NextMonday);

        var request = Draft(content);

        Assert.Equal([Flag.Inconsistency(LeaveErrors.PeriodInvalid)], request.Flags);
        Assert.Null(request.Duration);
        Assert.Equal(new PeriodBound(NextFriday, HalfDay.Morning), request.Start);
        Assert.Equal(new PeriodBound(NextMonday, HalfDay.Afternoon), request.End);
    }

    [Fact]
    public void A_draft_with_nothing_to_count_is_saved_with_an_inconsistency()
    {
        var request = Draft(Content(new LeaveTypePaidLeave(), NextSaturday, NextSunday));

        Assert.Equal([Flag.Inconsistency(LeaveErrors.DurationZero)], request.Flags);
        Assert.Null(request.Duration);
    }

    [Fact]
    public void A_draft_outside_the_calendar_is_saved_with_an_inconsistency()
    {
        var calendar = FakeLeaveCalendar.FullTimeUntil(NextMonday);

        var request = LeaveRequest.Draft(Request, Employee, Origin.Agent, NextWeek, Today, calendar).Value;

        Assert.Equal([Flag.Inconsistency(LeaveErrors.PeriodOutsideCalendar)], request.Flags);
        Assert.Null(request.Duration);
    }

    [Fact]
    public void A_draft_starting_in_the_past_is_saved_with_an_inconsistency()
    {
        var request = Draft(Content(new LeaveTypePaidLeave(), Today.AddDays(-1), Today));

        Assert.Equal([Flag.Inconsistency(LeaveErrors.BackdatingForbidden)], request.Flags);
        Assert.NotNull(request.Duration);
    }

    [Fact]
    public void A_draft_reports_all_its_inconsistencies_after_its_uncertainties()
    {
        var content = Content(new LeaveTypeRtt(), Today.AddDays(-1), Today.AddDays(-3)) with { Assumptions = ["YEAR_INFERRED"] };

        var request = Draft(content);

        Assert.Equal(
            [
                Flag.Uncertainty("YEAR_INFERRED"),
                Flag.Inconsistency(LeaveErrors.PeriodInvalid),
                Flag.Inconsistency(LeaveErrors.BackdatingForbidden),
            ],
            request.Flags);
    }

    [Fact]
    public void A_draft_of_a_late_sick_leave_carries_the_warning()
    {
        var request = Draft(Content(new LeaveTypeSickLeave(), Today.AddDays(-3), Today));

        Assert.Equal([Warning.LateDeclaration], request.Warnings);
        Assert.Empty(request.Flags);
    }

    // Comment (R6): the only rule that refuses a draft, as an invalid comment cannot exist.

    [Fact]
    public void A_sick_leave_draft_with_a_comment_is_refused()
    {
        var content = Content(new LeaveTypeSickLeave(), NextMonday, NextFriday) with { Comment = "Grippe" };

        var result = LeaveRequest.Draft(Request, Employee, Origin.Manual, content, Today, Calendar);

        Assert.Equal("comment.forbidden_for_sick_leave", result.Error?.Code);
    }

    [Fact]
    public void A_draft_with_a_too_long_comment_is_refused()
    {
        var content = NextWeek with { Comment = new string('a', 201) };

        var result = LeaveRequest.Draft(Request, Employee, Origin.Manual, content, Today, Calendar);

        Assert.Equal("comment.too_long", result.Error?.Code);
    }

    [Fact]
    public void The_content_of_a_draft_does_not_reveal_its_comment_in_the_logs()
    {
        var content = NextWeek with { Comment = "Vacances en famille" };

        Assert.DoesNotContain("Vacances", $"{content}");
    }

    // Draft update.

    [Fact]
    public void Updating_replaces_the_content_of_the_draft()
    {
        var request = Given(Drafted(NextWeek));

        var result = request.Update(Content(new LeaveTypeRtt(), NextMonday, NextMonday), Today, Calendar);

        Assert.True(result.IsSuccess);
        var updated = Assert.IsType<LeaveRequestUpdated>(Assert.Single(request.UncommittedEvents));
        Assert.Equal(Request, updated.LeaveRequestId);
        Assert.Equal(new LeaveTypeRtt(), request.Type);
        Assert.Equal(new PeriodBound(NextMonday, HalfDay.Afternoon), request.End);
        Assert.Equal(1m, request.Duration?.Total);
        Assert.Equal(LeaveRequestStatus.Draft, request.Status);
    }

    [Fact]
    public void Updating_replaces_the_flags()
    {
        var request = Given(Drafted(Content(new LeaveTypePaidLeave(), NextFriday, NextMonday) with { Assumptions = ["YEAR_INFERRED"] }));

        request.Update(NextWeek with { Assumptions = ["TYPE_INFERRED"] }, Today, Calendar);

        Assert.Equal([Flag.Uncertainty("TYPE_INFERRED")], request.Flags);
    }

    [Fact]
    public void Updating_can_save_an_invalid_draft()
    {
        var request = Given(Drafted(NextWeek));

        var result = request.Update(Content(new LeaveTypePaidLeave(), NextFriday, NextMonday), Today, Calendar);

        Assert.True(result.IsSuccess);
        Assert.Equal([Flag.Inconsistency(LeaveErrors.PeriodInvalid)], request.Flags);
        Assert.Null(request.Duration);
    }

    [Fact]
    public void Updating_refreshes_the_warnings()
    {
        var request = Given(Drafted(Content(new LeaveTypeSickLeave(), Today.AddDays(-3), Today)));

        request.Update(Content(new LeaveTypeSickLeave(), Today.AddDays(-1), Today), Today, Calendar);

        Assert.Empty(request.Warnings);
    }

    [Fact]
    public void Updating_keeps_the_identity_and_the_origin()
    {
        var request = Given(Drafted(NextWeek, Origin.Agent));

        request.Update(NextWeek, Today, Calendar);

        Assert.Equal(Request, request.Id);
        Assert.Equal(Employee, request.EmployeeId);
        Assert.Equal(Origin.Agent, request.Origin);
    }

    [Fact]
    public void Changing_a_draft_into_a_sick_leave_drops_its_comment()
    {
        var request = Given(Drafted(NextWeek with { Comment = "Vacances en famille" }));

        var result = request.Update(Content(new LeaveTypeSickLeave(), NextMonday, NextFriday), Today, Calendar);

        Assert.True(result.IsSuccess);
        Assert.Null(request.Comment);
    }

    [Fact]
    public void Changing_a_draft_into_a_sick_leave_while_keeping_its_comment_is_refused()
    {
        var request = Given(Drafted(NextWeek with { Comment = "Vacances en famille" }));

        var result = request.Update(Content(new LeaveTypeSickLeave(), NextMonday, NextFriday) with { Comment = "Vacances en famille" }, Today, Calendar);

        Assert.Equal("comment.forbidden_for_sick_leave", result.Error?.Code);
        Assert.Empty(request.UncommittedEvents);
    }

    [Fact]
    public void A_submitted_request_cannot_be_updated()
    {
        var request = Given(Drafted(NextWeek), Submitted());

        var result = request.Update(NextWeek, Today, Calendar);

        Assert.Equal("request.invalid_status", result.Error?.Code);
        Assert.Empty(request.UncommittedEvents);
    }

    [Fact]
    public void A_cancelled_request_cannot_be_updated()
    {
        var request = Given(Drafted(NextWeek), new LeaveRequestCancelled(Request));

        var result = request.Update(NextWeek, Today, Calendar);

        Assert.Equal("request.invalid_status", result.Error?.Code);
    }

    // Submission.

    [Fact]
    public void Submitting_raises_the_duration_of_the_request()
    {
        var request = Given(Drafted(NextWeek));

        var result = request.Submit(Today, Calendar);

        Assert.True(result.IsSuccess);
        var submitted = Assert.IsType<LeaveRequestSubmitted>(Assert.Single(request.UncommittedEvents));
        Assert.Equal(Request, submitted.LeaveRequestId);
        Assert.Equal(5m, submitted.Duration.Total);
        Assert.Empty(submitted.Warnings);
        Assert.Equal(LeaveRequestStatus.Submitted, request.Status);
    }

    [Fact]
    public void The_duration_is_counted_again_on_submission()
    {
        var request = Given(Drafted(NextWeek));

        request.Submit(Today, FakeLeaveCalendar.FullTime(NextFriday));

        Assert.Equal(4m, request.Duration?.Total);
    }

    [Fact]
    public void An_invalid_period_refuses_the_submission()
    {
        var request = Given(Drafted(Content(new LeaveTypePaidLeave(), NextFriday, NextMonday)));

        var result = request.Submit(Today, Calendar);

        Assert.Equal("period.invalid", result.Error?.Code);
        Assert.Empty(request.UncommittedEvents);
        Assert.Equal(LeaveRequestStatus.Draft, request.Status);
    }

    [Fact]
    public void A_period_outside_the_calendar_refuses_the_submission()
    {
        var request = Given(Drafted(NextWeek));

        var result = request.Submit(Today, FakeLeaveCalendar.FullTimeUntil(NextMonday));

        Assert.Equal("period.outside_calendar", result.Error?.Code);
        Assert.Empty(request.UncommittedEvents);
    }

    [Fact]
    public void Nothing_to_count_refuses_the_submission()
    {
        var request = Given(Drafted(Content(new LeaveTypePaidLeave(), NextSaturday, NextSunday)));

        var result = request.Submit(Today, Calendar);

        Assert.Equal("duration.zero", result.Error?.Code);
    }

    [Fact]
    public void A_start_date_that_passed_since_the_draft_refuses_the_submission()
    {
        var request = Given(Drafted(NextWeek));

        var result = request.Submit(NextMonday.AddDays(1), Calendar);

        Assert.Equal("backdating.forbidden", result.Error?.Code);
    }

    [Fact]
    public void A_late_sick_leave_is_submitted_with_the_warning()
    {
        var request = Given(Drafted(Content(new LeaveTypeSickLeave(), Today.AddDays(-3), Today)));

        var result = request.Submit(Today, Calendar);

        Assert.True(result.IsSuccess);
        var submitted = Assert.IsType<LeaveRequestSubmitted>(Assert.Single(request.UncommittedEvents));
        Assert.Equal([Warning.LateDeclaration], submitted.Warnings);
        Assert.Equal([Warning.LateDeclaration], request.Warnings);
    }

    [Fact]
    public void A_sick_leave_that_became_late_since_the_draft_gets_the_warning()
    {
        var request = Given(Drafted(Content(new LeaveTypeSickLeave(), Today, Today)));
        Assert.Empty(request.Warnings);

        request.Submit(Today.AddDays(3), Calendar);

        Assert.Equal([Warning.LateDeclaration], request.Warnings);
    }

    [Fact]
    public void Replaying_a_submission_raises_nothing()
    {
        var request = Given(Drafted(NextWeek), Submitted());

        var result = request.Submit(Today, Calendar);

        Assert.Equal("request.invalid_status", result.Error?.Code);
        Assert.Empty(request.UncommittedEvents);
    }

    [Fact]
    public void A_cancelled_request_cannot_be_submitted()
    {
        var request = Given(Drafted(NextWeek), new LeaveRequestCancelled(Request));

        var result = request.Submit(Today, Calendar);

        Assert.Equal("request.invalid_status", result.Error?.Code);
    }

    // Cancellation (R7): nothing is deleted.

    [Fact]
    public void A_draft_can_be_cancelled()
    {
        var request = Given(Drafted(NextWeek));

        var result = request.Cancel(Today);

        Assert.True(result.IsSuccess);
        Assert.Equal([new LeaveRequestCancelled(Request)], request.UncommittedEvents);
        Assert.Equal(LeaveRequestStatus.Cancelled, request.Status);
    }

    [Fact]
    public void A_draft_can_be_cancelled_after_its_first_day()
    {
        var request = Given(Drafted(NextWeek));

        var result = request.Cancel(NextMonday.AddDays(1));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void An_invalid_draft_can_be_cancelled()
    {
        var request = Given(Drafted(Content(new LeaveTypePaidLeave(), NextFriday, NextMonday)));

        var result = request.Cancel(Today);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void A_submitted_request_can_be_cancelled_until_the_day_before_its_start()
    {
        var request = Given(Drafted(NextWeek), Submitted());

        var result = request.Cancel(NextMonday.AddDays(-1));

        Assert.True(result.IsSuccess);
        Assert.Equal([new LeaveRequestCancelled(Request)], request.UncommittedEvents);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(30)]
    public void A_submitted_request_cannot_be_cancelled_once_started(int daysAfterStart)
    {
        var request = Given(Drafted(NextWeek), Submitted());

        var result = request.Cancel(NextMonday.AddDays(daysAfterStart));

        Assert.Equal("cancellation.leave_started", result.Error?.Code);
        Assert.Empty(request.UncommittedEvents);
        Assert.Equal(LeaveRequestStatus.Submitted, request.Status);
    }

    [Fact]
    public void A_request_starting_in_the_afternoon_has_started_from_the_morning_of_that_day()
    {
        var content = NextWeek with { Start = new PeriodBound(NextMonday, HalfDay.Afternoon) };
        var request = Given(Drafted(content), Submitted());

        var result = request.Cancel(NextMonday);

        Assert.Equal("cancellation.leave_started", result.Error?.Code);
    }

    [Fact]
    public void A_cancelled_request_cannot_be_cancelled_again()
    {
        var request = Given(Drafted(NextWeek), new LeaveRequestCancelled(Request));

        var result = request.Cancel(Today);

        Assert.Equal("request.invalid_status", result.Error?.Code);
        Assert.Empty(request.UncommittedEvents);
    }

    // Replay and audit (C17).

    [Fact]
    public void Replaying_the_events_gives_the_same_request()
    {
        var content = NextWeek with { Comment = "Vacances en famille", Assumptions = ["YEAR_INFERRED"] };
        var request = LeaveRequest.Draft(Request, Employee, Origin.Agent, content, Today, Calendar).Value;
        request.Update(content with { End = new PeriodBound(NextMonday.AddDays(1), HalfDay.Morning) }, Today, Calendar);
        request.Submit(Today, Calendar);

        var replayed = LeaveRequest.FromHistory(request.UncommittedEvents);

        Assert.Equal(request.Id, replayed.Id);
        Assert.Equal(Employee, replayed.EmployeeId);
        Assert.Equal(Origin.Agent, replayed.Origin);
        Assert.Equal(LeaveRequestStatus.Submitted, replayed.Status);
        Assert.Equal(new LeaveTypePaidLeave(), replayed.Type);
        Assert.Equal(request.Start, replayed.Start);
        Assert.Equal(request.End, replayed.End);
        Assert.Equal("Vacances en famille", replayed.Comment?.Text);
        Assert.Equal(1.5m, replayed.Duration?.Total);
        Assert.Equal([Flag.Uncertainty("YEAR_INFERRED")], replayed.Flags);
        Assert.Empty(replayed.Warnings);
        Assert.Equal(3, replayed.Version);
        Assert.Empty(replayed.UncommittedEvents);
    }

    [Fact]
    public void Every_action_stays_in_the_stream()
    {
        var request = LeaveRequest.Draft(Request, Employee, Origin.Manual, NextWeek, Today, Calendar).Value;
        request.Update(NextWeek, Today, Calendar);
        request.Submit(Today, Calendar);
        request.Cancel(Today);

        Assert.Collection(
            request.UncommittedEvents,
            domainEvent => Assert.IsType<LeaveRequestDrafted>(domainEvent),
            domainEvent => Assert.IsType<LeaveRequestUpdated>(domainEvent),
            domainEvent => Assert.IsType<LeaveRequestSubmitted>(domainEvent),
            domainEvent => Assert.IsType<LeaveRequestCancelled>(domainEvent));
    }

    /// <summary>From the morning of the first day to the afternoon of the last one.</summary>
    private static DraftContent Content(LeaveType type, DateOnly first, DateOnly last) =>
        new(type, new PeriodBound(first, HalfDay.Morning), new PeriodBound(last, HalfDay.Afternoon), null, []);

    private static LeaveRequest Draft(DraftContent content) =>
        LeaveRequest.Draft(Request, Employee, Origin.Manual, content, Today, Calendar).Value;

    /// <summary>The event that drafting this content raises, to use as a past event.</summary>
    private static IDomainEvent Drafted(DraftContent content, Origin origin = Origin.Manual) =>
        LeaveRequest.Draft(Request, Employee, origin, content, Today, Calendar).Value.UncommittedEvents[0];

    /// <summary>The submission of <see cref="NextWeek"/>, whatever the drafted content: only the status matters to the tests using it.</summary>
    private static LeaveRequestSubmitted Submitted() => new(Request, Draft(NextWeek).Duration!, []);

    private static LeaveRequest Given(params IDomainEvent[] history) => LeaveRequest.FromHistory(history);
}

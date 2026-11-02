using System.Text.Json;
using HrAdministration.Application.EventSourcing;
using HrAdministration.Domain.Balances;
using HrAdministration.Domain.Leaves;
using HrAdministration.Infrastructure.EventSourcing;
using SharedKernel;
using static HrAdministration.Infrastructure.Tests.EventSourcing.SampleEvents;

namespace HrAdministration.Infrastructure.Tests.EventSourcing;

public sealed class EventSerializerTests
{
    // Stored types.

    [Fact]
    public void Every_domain_event_can_be_stored()
    {
        var domainEvents = typeof(LeaveRequestDrafted).Assembly.GetTypes()
            .Where(type => type.IsAssignableTo(typeof(IDomainEvent)) && type is { IsClass: true, IsAbstract: false })
            .Select(type => type.Name);

        Assert.Equal(domainEvents.Order(), EventSerializer.StoredTypeNames.Order());
    }

    [Fact]
    public void The_samples_cover_every_stored_type()
    {
        Assert.Equal(EventSerializer.StoredTypeNames.Order(), OfEveryType().Select(EventSerializer.TypeName).Distinct().Order());
    }

    [Fact]
    public void An_event_type_that_is_not_mapped_is_a_bug()
    {
        Assert.Throws<InvalidOperationException>(() => EventSerializer.TypeName(new UnmappedEvent()));
    }

    [Fact]
    public void A_stored_type_that_is_unknown_is_a_bug()
    {
        Assert.Throws<InvalidOperationException>(() => EventSerializer.Deserialize("LeaveRequestRenamed", "{}"));
    }

    // Round trip.

    [Fact]
    public void Every_event_survives_a_round_trip()
    {
        Assert.All(OfEveryType(), domainEvent =>
        {
            var data = EventSerializer.SerializeData(domainEvent);

            var read = EventSerializer.Deserialize(EventSerializer.TypeName(domainEvent), data);

            Assert.IsType(domainEvent.GetType(), read);
            Assert.Equal(data, EventSerializer.SerializeData(read));
        });
    }

    [Fact]
    public void A_draft_keeps_its_whole_content()
    {
        var drafted = (LeaveRequestDrafted)Draft(PaidLeave).UncommittedEvents[0];

        var read = (LeaveRequestDrafted)EventSerializer.Deserialize(nameof(LeaveRequestDrafted), EventSerializer.SerializeData(drafted));

        Assert.Equal(drafted.LeaveRequestId, read.LeaveRequestId);
        Assert.Equal(EmployeeId, read.EmployeeId);
        Assert.Equal(Origin.Agent, read.Origin);
        Assert.Equal(new LeaveTypePaidLeave(), read.Type);
        Assert.Equal(Monday, read.Start);
        Assert.Equal(Friday, read.End);
        Assert.Equal(drafted.Comment, read.Comment);
        Assert.Equal(5m, read.Duration!.Total);
        Assert.Equal(drafted.Duration!.Days, read.Duration.Days);
        Assert.Equal(drafted.Flags, read.Flags);
        Assert.Equal(drafted.Warnings, read.Warnings);
    }

    [Fact]
    public void A_sick_leave_stays_sensitive_and_without_comment()
    {
        var drafted = (LeaveRequestDrafted)Draft(new DraftContent(new LeaveTypeSickLeave(), Monday, Friday, null, [])).UncommittedEvents[0];

        var read = (LeaveRequestDrafted)EventSerializer.Deserialize(nameof(LeaveRequestDrafted), EventSerializer.SerializeData(drafted));

        Assert.True(read.Type.IsSensitive);
        Assert.Null(read.Comment);
    }

    [Fact]
    public void A_draft_without_duration_keeps_it_missing()
    {
        var invalid = new DraftContent(new LeaveTypePaidLeave(), Friday, Monday, null, []);
        var drafted = (LeaveRequestDrafted)Draft(invalid).UncommittedEvents[0];

        var read = (LeaveRequestDrafted)EventSerializer.Deserialize(nameof(LeaveRequestDrafted), EventSerializer.SerializeData(drafted));

        Assert.Null(read.Duration);
    }

    [Fact]
    public void A_stored_comment_replays_even_if_it_breaks_todays_rules()
    {
        var tooLong = new string('a', Comment.MaxLength + 1);
        var data = EventSerializer.SerializeData(Draft(PaidLeave).UncommittedEvents[0])
            .Replace(JsonEncodedText.Encode(PaidLeave.Comment!).ToString(), tooLong, StringComparison.Ordinal);

        var read = (LeaveRequestDrafted)EventSerializer.Deserialize(nameof(LeaveRequestDrafted), data);

        Assert.Equal(tooLong, read.Comment!.Text);
    }

    // Format of the stored history: an event written today must still be read tomorrow.

    [Fact]
    public void A_stored_draft_can_still_be_read()
    {
        const string stored = """
            {
              "leave_request_id": "7f3b5c1e-0d7a-4f60-9a55-0a4f8a3c1b11",
              "employee_id": "2a6e4d90-3c15-4b7e-8d11-5e9f0c7a2b22",
              "origin": "agent",
              "type": "PaidLeave",
              "start": { "date": "2026-11-09", "half_day": "morning" },
              "end": { "date": "2026-11-09", "half_day": "afternoon" },
              "comment": "Vacances",
              "duration": [{ "date": "2026-11-09", "kind": "working_day", "days": 1 }],
              "flags": [{ "kind": "uncertainty", "code": "YEAR_INFERRED", "message": null }],
              "warnings": [{ "code": "declaration.late", "message": "Tardif." }]
            }
            """;

        var read = (LeaveRequestDrafted)EventSerializer.Deserialize("LeaveRequestDrafted", stored);

        Assert.Equal(Origin.Agent, read.Origin);
        Assert.Equal(new LeaveTypePaidLeave(), read.Type);
        Assert.Equal(new PeriodBound(new DateOnly(2026, 11, 9), HalfDay.Afternoon), read.End);
        Assert.Equal("Vacances", read.Comment!.Text);
        Assert.Equal(new CountedDay(new DateOnly(2026, 11, 9), DayKind.WorkingDay, 1m), Assert.Single(read.Duration!.Days));
        Assert.Equal(Flag.Uncertainty("YEAR_INFERRED"), Assert.Single(read.Flags));
        Assert.Equal(new Warning("declaration.late", "Tardif."), Assert.Single(read.Warnings));
    }

    [Fact]
    public void A_stored_entitlement_can_still_be_read()
    {
        const string stored = """
            {
              "balance_id": { "employee_id": "2a6e4d90-3c15-4b7e-8d11-5e9f0c7a2b22", "type": "rtt", "year": 2026 },
              "days": 10.5,
              "current_year_only": true
            }
            """;

        var read = EventSerializer.Deserialize("EntitlementGranted", stored);

        var employeeId = Guid.Parse("2a6e4d90-3c15-4b7e-8d11-5e9f0c7a2b22");
        Assert.Equal(new EntitlementGranted(new LeaveBalanceId(employeeId, BalanceType.Rtt, 2026), 10.5m, true), read);
    }

    [Fact]
    public void Metadata_hold_the_actor_the_origin_the_correlation_and_the_schema_version()
    {
        var correlationId = Guid.NewGuid();

        var metadata = EventSerializer.SerializeMetadata(new EventMetadata(Actor.Employee, Origin.Agent, correlationId));

        using var json = JsonDocument.Parse(metadata);
        Assert.Equal("employee", json.RootElement.GetProperty("actor").GetString());
        Assert.Equal("agent", json.RootElement.GetProperty("origin").GetString());
        Assert.Equal(correlationId, json.RootElement.GetProperty("correlation_id").GetGuid());
        Assert.Equal(1, json.RootElement.GetProperty("schema_version").GetInt32());
    }

    private sealed record UnmappedEvent : IDomainEvent;
}

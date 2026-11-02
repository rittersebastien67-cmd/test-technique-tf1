using System.Text.Json;
using System.Text.Json.Serialization;
using HrAdministration.Application.EventSourcing;
using HrAdministration.Domain.Balances;
using HrAdministration.Domain.Leaves;
using SharedKernel;

namespace HrAdministration.Infrastructure.EventSourcing;

/// <summary>
/// Maps domain events to the <c>type</c>, <c>data</c> and <c>metadata</c> columns. Domain events know nothing about serialization (section 9),
/// so everything is decided here. The names written here are in the stored history: changing one needs a new schema version.
/// </summary>
public static class EventSerializer
{
    /// <summary>Version of the format of the events written today (section 2.1 of the architecture).</summary>
    public const int SchemaVersion = 1;

    private static readonly IReadOnlyDictionary<string, Type> EventTypes = new[]
    {
        typeof(EntitlementGranted),
        typeof(DaysDeducted),
        typeof(DaysCredited),
        typeof(LeaveRequestDrafted),
        typeof(LeaveRequestUpdated),
        typeof(LeaveRequestSubmitted),
        typeof(LeaveRequestCancelled),
        typeof(LeaveRequestSuperseded),
        typeof(LeaveRequestCarriedOver),
    }.ToDictionary(type => type.Name);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false),
            new LeaveTypeJsonConverter(),
            new CommentJsonConverter(),
            new CountedDurationJsonConverter(),
        },
    };

    public static IEnumerable<string> StoredTypeNames => EventTypes.Keys;

    public static string TypeName(IDomainEvent domainEvent)
    {
        var name = domainEvent.GetType().Name;
        return EventTypes.ContainsKey(name)
            ? name
            : throw new InvalidOperationException($"The event type {name} cannot be stored.");
    }

    public static string SerializeData(IDomainEvent domainEvent) =>
        JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), Options);

    public static IDomainEvent Deserialize(string typeName, string data)
    {
        if (!EventTypes.TryGetValue(typeName, out var type))
        {
            throw new InvalidOperationException($"The stored event type {typeName} is unknown.");
        }

        return (IDomainEvent)JsonSerializer.Deserialize(data, type, Options)!;
    }

    public static string SerializeMetadata(EventMetadata metadata) =>
        JsonSerializer.Serialize(new StoredMetadata(metadata.Actor, metadata.Origin, metadata.CorrelationId, SchemaVersion), Options);

    private sealed record StoredMetadata(Actor Actor, Origin Origin, Guid CorrelationId, int SchemaVersion);
}

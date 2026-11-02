using System.Text.Json;
using System.Text.Json.Serialization;
using HrAdministration.Domain.Leaves;

namespace HrAdministration.Infrastructure.EventSourcing;

/// <summary>Stores a leave type by its name, as the Domain closes its hierarchy without any serialization attribute (AD58).</summary>
internal sealed class LeaveTypeJsonConverter : JsonConverter<LeaveType>
{
    public static LeaveType Parse(string? name) => name switch
    {
        "PaidLeave" => new LeaveTypePaidLeave(),
        "Rtt" => new LeaveTypeRtt(),
        "SickLeave" => new LeaveTypeSickLeave(),
        "UnpaidLeave" => new LeaveTypeUnpaidLeave(),
        _ => throw new JsonException("Unknown leave type."),
    };

    public override LeaveType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Parse(reader.GetString());

    public override void Write(Utf8JsonWriter writer, LeaveType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Name);
}

using System.Text.Json;
using System.Text.Json.Serialization;
using HrAdministration.Domain.Leaves;

namespace HrAdministration.Infrastructure.EventSourcing;

/// <summary>Stores a duration as its days: the total is computed from them.</summary>
internal sealed class CountedDurationJsonConverter : JsonConverter<CountedDuration>
{
    public override CountedDuration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(JsonSerializer.Deserialize<List<CountedDay>>(ref reader, options) ?? throw new JsonException("A duration needs its days."));

    public override void Write(Utf8JsonWriter writer, CountedDuration value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.Days, options);
}

using System.Text.Json;
using System.Text.Json.Serialization;
using HrAdministration.Domain.Leaves;

namespace HrAdministration.Infrastructure.EventSourcing;

/// <summary>Stores a comment as plain text. Reading it does not check the rules again: what is in a stream has already been accepted.</summary>
internal sealed class CommentJsonConverter : JsonConverter<Comment>
{
    public override Comment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString() ?? throw new JsonException("A comment cannot be null."));

    public override void Write(Utf8JsonWriter writer, Comment value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Text);
}

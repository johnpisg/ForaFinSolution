using System.Text.Json;
using System.Text.Json.Serialization;

namespace ForaFinServices.Infrastructure.Converter;

public sealed class PaddedIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();

            if (int.TryParse(value, out var result))
                return result;

            throw new JsonException($"Invalid padded int value: {value}");
        }

        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetInt32();

        throw new JsonException("Unexpected token type for int");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

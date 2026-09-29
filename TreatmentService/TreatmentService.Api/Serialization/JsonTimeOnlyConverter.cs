using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TreatmentService.Api.Serialization;

// The service's only client is the monolith, which formats times for the frontend itself.
// On this internal boundary times travel with full precision, so nothing stored is lost.
public sealed class JsonTimeOnlyConverter : JsonConverter<TimeOnly>
{
    private const string Format = "HH:mm:ss.FFFFFFF";

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String
            && TimeSpan.TryParse(reader.GetString(), CultureInfo.InvariantCulture, out var timeSpan))
        {
            return TimeOnly.FromTimeSpan(timeSpan);
        }

        throw new JsonException("Unable to parse TimeOnly from JSON.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}

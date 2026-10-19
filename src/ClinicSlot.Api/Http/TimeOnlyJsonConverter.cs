using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClinicSlot.Api.Http;

/// <summary>Reads wall-clock times as "HH:mm" (or "HH:mm:ss") and writes them as "HH:mm".</summary>
public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] Formats = ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF"];

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (text is not null
            && TimeOnly.TryParseExact(text, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
        {
            return value;
        }

        throw new JsonException($"'{text}' is not a valid time of day; use HH:mm.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString("HH:mm", CultureInfo.InvariantCulture));
    }
}

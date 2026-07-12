using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductCacheApi.Config;

/// <summary>
/// Serializes every <see cref="DateTime"/> as UTC (ISO-8601 with a trailing 'Z').
/// MySQL columns come back with <see cref="DateTimeKind.Unspecified"/>, which otherwise
/// serializes without the 'Z' and produces an inconsistent contract across endpoints.
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}

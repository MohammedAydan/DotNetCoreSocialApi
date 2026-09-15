using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Social.API.Serialization
{
    /// <summary>
    /// Enforces strict ISO 8601 UTC date serialization with explicit 'Z' designator
    /// and normalized UTC deserialization across all API endpoints.
    /// </summary>
    public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                return reader.GetDateTime();
            }

            var dateStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(dateStr))
            {
                return default;
            }

            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedDate))
            {
                return DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }

            return DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utcValue = value.Kind switch
            {
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => value
            };

            writer.WriteStringValue(utcValue.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Enforces strict ISO 8601 UTC date serialization with explicit 'Z' designator
    /// and normalized UTC deserialization for nullable DateTime fields.
    /// </summary>
    public class NullableUtcDateTimeJsonConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                return reader.GetDateTime();
            }

            var dateStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(dateStr))
            {
                return null;
            }

            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedDate))
            {
                return DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }

            return DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            var utcValue = value.Value.Kind switch
            {
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => value.Value
            };

            writer.WriteStringValue(utcValue.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog.Events;

namespace ClipMate.Infrastructure
{
    internal class LogEventLevelJsonConverter : JsonConverter<LogEventLevel>
    {
        public override LogEventLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                if (!string.IsNullOrWhiteSpace(value) &&
                    Enum.TryParse<LogEventLevel>(value, true, out var parsed))
                {
                    return LogLevelPolicy.Normalize(parsed);
                }
            }
            else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number))
            {
                var parsed = (LogEventLevel)number;
                if (Enum.IsDefined(typeof(LogEventLevel), parsed))
                {
                    return LogLevelPolicy.Normalize(parsed);
                }
            }

            return LogEventLevel.Information;
        }

        public override void Write(Utf8JsonWriter writer, LogEventLevel value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(LogLevelPolicy.Normalize(value).ToString());
        }
    }
}

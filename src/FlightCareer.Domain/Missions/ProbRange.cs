using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlightCareer.Domain.Missions
{
    [JsonConverter(typeof(ProbRangeJsonConverter))]
    public class ProbRange
    {
        public double Weight { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }

        public ProbRange() { }

        public ProbRange(double w, int min, int max)
        {
            Weight = w;
            Min = min;
            Max = max;
        }
    }

    public sealed class ProbRangeJsonConverter : JsonConverter<ProbRange>
    {
        public override ProbRange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();
                double w = reader.GetDouble();
                reader.Read();
                int min = reader.GetInt32();
                reader.Read();
                int max = reader.GetInt32();
                reader.Read(); // EndArray
                return new ProbRange(w, min, max);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;
                double w = root.TryGetProperty("weight", out var wEl) ? wEl.GetDouble() : 0.0;
                int min = root.TryGetProperty("min", out var minEl) ? minEl.GetInt32() : 0;
                int max = root.TryGetProperty("max", out var maxEl) ? maxEl.GetInt32() : 0;
                return new ProbRange(w, min, max);
            }

            throw new JsonException($"Token inesperado para ProbRange: {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, ProbRange value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Weight);
            writer.WriteNumberValue(value.Min);
            writer.WriteNumberValue(value.Max);
            writer.WriteEndArray();
        }
    }
}

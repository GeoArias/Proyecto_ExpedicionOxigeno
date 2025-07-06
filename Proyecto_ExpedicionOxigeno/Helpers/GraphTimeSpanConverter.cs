using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace Proyecto_ExpedicionOxigeno.Helpers
{
    /// <summary>
    /// Converts between ISO 8601 duration format (like "PT30M") and .NET TimeSpan
    /// </summary>
    public class GraphTimeSpanConverter : JsonConverter
    {
        // Regular expression to parse ISO 8601 duration format
        private static readonly Regex DurationRegex = new Regex(
            @"^P(?:(\d+)D)?T?(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?$",
            RegexOptions.Compiled);

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeSpan) || objectType == typeof(TimeSpan?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException($"Unexpected token type: {reader.TokenType}. Expected String.");
            }

            string durationString = reader.Value.ToString();
            if (string.IsNullOrEmpty(durationString))
            {
                return objectType == typeof(TimeSpan?) ? (TimeSpan?)null : TimeSpan.Zero;
            }

            // Check if it's a negative duration
            bool isNegative = durationString.StartsWith("-");
            if (isNegative)
            {
                // Remove the negative sign for parsing
                durationString = durationString.Substring(1);
            }

            // Parse ISO 8601 duration format
            var match = DurationRegex.Match(durationString);
            if (!match.Success)
            {
                throw new JsonSerializationException($"Invalid duration format: {durationString}");
            }

            // Extract days, hours, minutes, seconds from regex groups
            int days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            int seconds = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;

            // Create TimeSpan and apply negative sign if needed
            var timeSpan = new TimeSpan(days, hours, minutes, seconds);
            return isNegative ? -timeSpan : timeSpan;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var timeSpan = (TimeSpan)value;

            // Convert TimeSpan to ISO 8601 duration format
            string duration = "PT";

            // Only include non-zero components
            if (timeSpan.Days > 0)
                duration = $"P{timeSpan.Days}DT";

            if (timeSpan.Hours > 0)
                duration += $"{timeSpan.Hours}H";

            if (timeSpan.Minutes > 0)
                duration += $"{timeSpan.Minutes}M";

            if (timeSpan.Seconds > 0)
                duration += $"{timeSpan.Seconds}S";

            // Handle zero duration
            if (duration == "PT")
                duration = "PT0S";

            writer.WriteValue(duration);
        }
    }
}
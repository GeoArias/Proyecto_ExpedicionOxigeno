using Microsoft.Kiota.Abstractions;
using Newtonsoft.Json;
using System;

namespace Proyecto_ExpedicionOxigeno.Helpers
{
    /// <summary>
    /// Converts between time strings (like "09:00:00.0000000") and Microsoft.Kiota.Abstractions.Time
    /// </summary>
    public class GraphTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Time) || objectType == typeof(Time?);
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

            string timeString = reader.Value.ToString();
            if (string.IsNullOrEmpty(timeString))
            {
                return null;
            }

            try
            {
                // Parse the time string (format: "HH:mm:ss.fffffff")
                if (TimeSpan.TryParse(timeString, out TimeSpan timeSpan))
                {
                    // Create a Time object with the parsed values
                    Time time = new Time(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

                    // Return the appropriate type based on whether the target is nullable
                    if (objectType == typeof(Time?))
                    {
                        return (Time?)time;
                    }

                    return time;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to parse time string: {timeString}");
                    throw new JsonSerializationException($"Failed to parse time string: {timeString}");
                }
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Error parsing time '{timeString}': {ex.Message}", ex);
            }

            throw new JsonSerializationException($"Invalid time format: {timeString}");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var time = (Time)value;

            // Format the time as "HH:mm:ss"
            string timeString = $"{time.Hour:D2}:{time.Minute:D2}:{time.Second:D2}";

            writer.WriteValue(timeString);
        }
    }
}
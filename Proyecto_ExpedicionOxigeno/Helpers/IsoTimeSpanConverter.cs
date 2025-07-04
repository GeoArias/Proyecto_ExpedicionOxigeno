using Newtonsoft.Json;
using System;
using System.Xml;

namespace Proyecto_ExpedicionOxigeno.Helpers
{
    public class IsoTimeSpanConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeSpan) || objectType == typeof(TimeSpan?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            string value = reader.Value.ToString();
            
            try
            {
                // Parse ISO 8601 duration format
                if (value.StartsWith("P", StringComparison.OrdinalIgnoreCase))
                {
                    return XmlConvert.ToTimeSpan(value);
                }
                // Fallback to regular parsing
                return TimeSpan.Parse(value);
            }
            catch
            {
                throw new JsonSerializationException($"Error converting value '{value}' to TimeSpan.");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            TimeSpan timeSpan = (TimeSpan)value;
            string isoValue = XmlConvert.ToString(timeSpan);
            writer.WriteValue(isoValue);
        }
    }
}
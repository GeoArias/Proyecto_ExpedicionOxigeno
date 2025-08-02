using System;
using Microsoft.Kiota.Abstractions;
using Newtonsoft.Json;

public class GraphDateConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Date) || objectType == typeof(Date?);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var value = reader.Value?.ToString();
        if (string.IsNullOrEmpty(value))
            return default(Date);

        // Parse "yyyy-MM-dd" format
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var dt))
        {
            return new Date(dt.Year, dt.Month, dt.Day);
        }

        throw new JsonSerializationException($"Cannot convert {value} to Microsoft.Kiota.Abstractions.Date");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is Date date)
        {
            writer.WriteValue($"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}");
        }
        else
        {
            writer.WriteNull();
        }
    }
}
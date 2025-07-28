using System;
using Newtonsoft.Json;
using Microsoft.Kiota.Abstractions;

public class KiotaDateConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Date) || objectType == typeof(Date?);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var dateStr = reader.Value as string;
        if (string.IsNullOrEmpty(dateStr))
            return null;

        // Parse "yyyy-MM-dd"
        if (DateTime.TryParse(dateStr, out var dt))
        {
            return new Date(dt.Year, dt.Month, dt.Day);
        }
        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var date = value as Date;
        if (date != null)
        {
            writer.WriteValue($"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}");
        }
        else
        {
            writer.WriteNull();
        }
    }
}
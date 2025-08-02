using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class IgnoreKiotaPropertiesResolver : DefaultContractResolver
{
    private static readonly HashSet<string> _kiotaInfraProperties = new HashSet<string>
    {
        "AdditionalData",
        "BackingStore"
    };

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
        // Exclude Kiota infrastructure properties
        props = props.Where(p => !_kiotaInfraProperties.Contains(p.PropertyName)).ToList();
        return props;
    }
}
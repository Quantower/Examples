// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using Newtonsoft.Json;
using System;
using System.Globalization;

namespace OKExV5Vendor.API.REST.JsonConverters;

public class JsonStringToLongOrDefaultConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => true;
    public override bool CanRead => true;
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        try
        {
            string str = reader.Value.ToString();

            if (!string.IsNullOrEmpty(str) && long.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out long value))
                return value;
        }
        catch { }

        return existingValue;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { }
}
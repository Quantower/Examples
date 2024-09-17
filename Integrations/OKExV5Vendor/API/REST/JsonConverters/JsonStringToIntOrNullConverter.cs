// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Newtonsoft.Json;
using System;
using System.Globalization;

namespace OKExV5Vendor.API.REST.JsonConverters;

public class JsonStringToIntOrNullConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => true;
    public override bool CanRead => true;
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        try
        {
            string str = reader.Value.ToString();

            if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return value;
        }
        catch { }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { }
}
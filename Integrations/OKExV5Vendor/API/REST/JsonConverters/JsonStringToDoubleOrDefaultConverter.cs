// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using System;
using System.Globalization;

namespace OKExV5Vendor.API.REST.JsonConverters
{
    public class JsonStringToDoubleOrDefaultConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(double) || objectType == typeof(double?);
        }
        public override bool CanRead => true;
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                string str = reader.Value.ToString();

                if (!string.IsNullOrEmpty(str) && double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    return value;
            }
            catch { }

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { }
    }
}

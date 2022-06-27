// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace OKExV5Vendor.API.REST.JsonConverters
{
    public class JsonStringToEnumOrDefaultConverter : StringEnumConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string str = reader.Value.ToString();

            if (!string.IsNullOrEmpty(str))
                return base.ReadJson(reader, objectType, existingValue, serializer);
            else
                return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { }
    }

}

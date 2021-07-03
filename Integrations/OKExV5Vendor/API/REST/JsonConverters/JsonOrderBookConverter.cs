using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKExV5Vendor.API.REST.Models;
using System;
using System.Collections.Generic;

namespace OKExV5Vendor.API.REST.JsonConverters
{
    class JsonOrderBookConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var priceItems = new List<OKExOrderBookItem>();

            try
            {
                var jArray = serializer.Deserialize<JArray>(reader);
                foreach (var item in jArray)
                {
                    var elements = item.Value<JArray>();
                    priceItems.Add(new OKExOrderBookItem()
                    {
                        Price = elements[0].Value<double>(),
                        Size = elements[1].Value<double>(),
                    });
                }
            }
            catch
            { }

            return priceItems.ToArray();
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { }

    }
}

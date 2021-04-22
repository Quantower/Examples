// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using BitfinexVendor.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace BitfinexVendor.API.JsonConverters
{
    class BitfinexTradeJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jArray = serializer.Deserialize<JArray>(reader);

            var result = new BitfinexTrade()
            {
                Timestamp = jArray[1].Value<long>(),
                Amount = jArray[2].Value<decimal>(),
                Price = jArray[3].Value<decimal>()
            };

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        { }
    }
}

// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters
{
    internal class BitfinexTradeJsonConverter : JsonConverter<BitfinexTrade>
    {
        public override BitfinexTrade ReadJson(JsonReader reader, Type objectType, BitfinexTrade existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jArray = serializer.Deserialize<JArray>(reader);

            var result = new BitfinexTrade
            {
                Timestamp = jArray[1].Value<long>(),
                Amount = jArray[2].Value<decimal>(),
                Price = jArray[3].Value<decimal>()
            };

            return result;
        }

        public override void WriteJson(JsonWriter writer, BitfinexTrade value, JsonSerializer serializer)
        { }
    }
}

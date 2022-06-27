// Copyright QUANTOWER LLC. © 2017-2022. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters
{
    internal class BitfinexUserTradeJsonConverter : JsonConverter<BitfinexUserTrade>
    {
        public override BitfinexUserTrade ReadJson(JsonReader reader, Type objectType, BitfinexUserTrade existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jArray = serializer.Deserialize<JArray>(reader);

            var result = new BitfinexUserTrade
            {
                Id = jArray[0].Value<long>(),
                Pair = jArray[1].Value<string>(),
                ExecutionTime = DateTimeOffset.FromUnixTimeMilliseconds(jArray[2].Value<long>()).UtcDateTime,
                OrderId = jArray[3].Value<long>(),
                Amount = jArray[4].Value<double>(),
                Price = jArray[5].Value<double>(),
                OrderType = jArray[6].Value<string>(),
                OrderPrice = jArray[7].Value<double>(),
                IsMaker = jArray[8].Value<int>() == 1,
                Fee = jArray[9].Value<double?>(),
                FeeCurrency = jArray[10].Value<string>()
            };

            return result;
        }

        public override void WriteJson(JsonWriter writer, BitfinexUserTrade value, JsonSerializer serializer)
        { }
    }
}
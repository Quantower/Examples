// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Bitfinex.API.JsonConverters;

internal class BitfinexCandleJsonConverter : JsonConverter<BitfinexCandle>
{
    public override BitfinexCandle ReadJson(JsonReader reader, Type objectType, BitfinexCandle existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jArray = serializer.Deserialize<JArray>(reader);

        var result = new BitfinexCandle
        {
            Timestamp = jArray[0].Value<long>(),
            Open = jArray[1].Value<decimal>(),
            Close = jArray[2].Value<decimal>(),
            High = jArray[3].Value<decimal>(),
            Low = jArray[4].Value<decimal>(),
            Volume = jArray[5].Value<decimal>()
        };

        return result;
    }

    public override void WriteJson(JsonWriter writer, BitfinexCandle value, JsonSerializer serializer) { }
}
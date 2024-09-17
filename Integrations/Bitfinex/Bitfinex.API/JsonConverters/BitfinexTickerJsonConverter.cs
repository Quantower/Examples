// Copyright QUANTOWER LLC. © 2017-2024. All rights reserved.

using System;
using Bitfinex.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API.JsonConverters;

internal class BitfinexTickerJsonConverter : JsonConverter<BitfinexTicker>
{
    public override void WriteJson(JsonWriter writer, BitfinexTicker value, JsonSerializer serializer) { }

    public override BitfinexTicker ReadJson(JsonReader reader, Type objectType, BitfinexTicker existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jArray = serializer.Deserialize<JArray>(reader);

        string pair = jArray[0].Value<string>();

        if (pair.StartsWith("t"))
            return new BitfinexTicker
            {
                Pair = pair[1..],
                Bid = jArray[1].Value<decimal>(),
                BidSize = jArray[2].Value<decimal>(),
                Ask = jArray[3].Value<decimal>(),
                AskSize = jArray[4].Value<decimal>(),
                DailyChange = jArray[5].Value<decimal>(),
                DailyChangePercent = jArray[6].Value<decimal>(),
                LastPrice = jArray[7].Value<decimal>(),
                Volume = jArray[8].Value<decimal>(),
                High = jArray[9].Value<decimal>(),
                Low = jArray[10].Value<decimal>()
            };
        else
            return new BitfinexTicker
            {
                Pair = pair[1..],
                Bid = jArray[2].Value<decimal>(),
                BidSize = jArray[4].Value<decimal>(),
                Ask = jArray[5].Value<decimal>(),
                AskSize = jArray[7].Value<decimal>(),
                DailyChange = jArray[8].Value<decimal>(),
                DailyChangePercent = jArray[9].Value<decimal>(),
                LastPrice = jArray[10].Value<decimal>(),
                Volume = jArray[11].Value<decimal>(),
                High = jArray[12].Value<decimal>(),
                Low = jArray[13].Value<decimal>()
            };
    }
}